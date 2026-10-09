using System.Collections.Concurrent;
using System.Diagnostics;
using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using ISession = Opc.Ua.Client.ISession;
using StatusCodes = Opc.Ua.StatusCodes;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>
    /// Đọc tag từ OPC UA server của Kepware. Tag Kepware có dạng "Channel.Device.Tag" (ví dụ
    /// "MHE PLC LS.LSE.EXTRUDER#1.CYL#1") và được truy cập bằng NodeId dạng chuỗi trong namespace
    /// cấu hình (<see cref="OpcUaOptions.NamespaceIndex"/>, thường là 2).
    ///
    /// Giữ MỘT phiên (session) dùng chung; khi lỗi thì huỷ phiên và tự tạo lại ở lần đọc kế tiếp.
    /// </summary>
    public sealed class OpcUaPlcDataReader : IPlcDataReader, IAsyncDisposable
    {
        // Thư viện OPC UA bản mới yêu cầu truyền ITelemetryContext (ghi log/đo đạc nội bộ của thư viện); dùng bản mặc định.
        private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });

        private readonly OpcUaOptions _options;
        private readonly ILogger<OpcUaPlcDataReader> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private ApplicationConfiguration? _configuration;
        private ISession? _session;

        // Chế độ subscription: Kepware tự quét tag rồi đẩy giá trị mới về, ReadAsync chỉ lấy từ bộ nhớ nên tag
        // chậm/lỗi (vd. tag của "MHE TOUCH" mỗi lần đọc đồng bộ mất ~9 giây) không chặn các tag khác.
        private Subscription? _subscription;
        private readonly Dictionary<string, MonitoredItem> _items = new();
        private readonly ConcurrentDictionary<string, PlcTagValue> _latest = new();
        private volatile bool _sessionFaulted;
        private string? _lastFailureMessage;
        private DateTime _lastFailureAt;

        public OpcUaPlcDataReader(IOptions<PlcConnectionOptions> options, ILogger<OpcUaPlcDataReader> logger)
        {
            _options = options.Value.OpcUa;
            _logger = logger;
        }

        public async Task<IReadOnlyDictionary<string, PlcTagValue>> ReadAsync(
            IReadOnlyCollection<string> tagIds, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, PlcTagValue>();
            if (tagIds.Count == 0)
            {
                return result;
            }

            await _gate.WaitAsync(cancellationToken);
            try
            {
                // Vừa thất bại gần đây: trả lỗi ngay thay vì lại chờ kết nối (tránh trang bị treo mỗi giây).
                if (_session is not { Connected: true }
                    && _lastFailureMessage is not null
                    && DateTime.UtcNow - _lastFailureAt < TimeSpan.FromMilliseconds(_options.RetryCooldownMs))
                {
                    throw new PlcConnectionException(_lastFailureMessage);
                }

                var session = await EnsureSessionAsync(cancellationToken);
                var ids = tagIds.ToList();

                if (_options.UseSubscription)
                {
                    var subscribed = await ReadSubscribedAsync(session, ids, cancellationToken);
                    _lastFailureMessage = null;
                    return subscribed;
                }

                var nodes = new ReadValueIdCollection();
                foreach (var tag in ids)
                {
                    nodes.Add(new ReadValueId
                    {
                        NodeId = new NodeId(tag, (ushort)_options.NamespaceIndex),
                        AttributeId = Attributes.Value
                    });
                }

                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                readTimeout.CancelAfter(_options.OperationTimeoutMs);
                var response = await session.ReadAsync(null, 0, TimestampsToReturn.Source, nodes, readTimeout.Token);

                for (var i = 0; i < ids.Count; i++)
                {
                    result[ids[i]] = ToTagValue(ids[i], response.Results[i]);
                }

                _lastFailureMessage = null;
                return result;
            }
            catch (PlcConnectionException)
            {
                throw; // lỗi đang trong thời gian nghỉ giữa các lần thử lại
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // người dùng huỷ yêu cầu — không phải lỗi kết nối
            }
            catch (Exception ex)
            {
                await DropSessionAsync();
                var reason = ex is OperationCanceledException ? "quá thời gian chờ" : ex.Message;
                if (ex is TimeoutException) reason = ex.Message;
                _lastFailureMessage = $"Không đọc được dữ liệu từ Kepware ({_options.EndpointUrl}): {reason}";
                _lastFailureAt = DateTime.UtcNow;
                _logger.LogWarning(ex, "Lỗi đọc OPC UA");
                throw new PlcConnectionException(_lastFailureMessage, ex);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static PlcTagValue ToTagValue(string tag, DataValue data)
        {
            if (StatusCode.IsBad(data.StatusCode) || data.Value is null)
            {
                return new PlcTagValue(tag, null, false, null, data.StatusCode.ToString());
            }

            try
            {
                var number = Convert.ToDouble(data.Value, System.Globalization.CultureInfo.InvariantCulture);
                var good = StatusCode.IsGood(data.StatusCode);
                var timestamp = data.SourceTimestamp == DateTime.MinValue ? (DateTime?)null : data.SourceTimestamp.ToLocalTime();
                return new PlcTagValue(tag, number, good, timestamp, good ? null : data.StatusCode.ToString());
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                return new PlcTagValue(tag, null, false, null, "Giá trị không phải kiểu số");
            }
        }

        private async Task<ISession> EnsureSessionAsync(CancellationToken cancellationToken)
        {
            if (_session is { Connected: true })
            {
                return _session;
            }

            await DropSessionAsync();

            // Giới hạn CỨNG thời gian kết nối: thư viện OPC UA không phải lúc nào cũng tôn trọng việc huỷ
            // (khi server tắt có thể mất nhiều giây để từ bỏ), nên nếu quá hạn thì ngừng chờ và bỏ mặc
            // lần kết nối đó — nếu nó hoàn tất muộn thì phiên được đóng lại, không để rò rỉ.
            var connectTask = ConnectAsync(cancellationToken);
            var timeout = Task.Delay(_options.ConnectTimeoutMs, cancellationToken);
            if (await Task.WhenAny(connectTask, timeout) != connectTask)
            {
                _ = connectTask.ContinueWith(
                    async t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            try { await t.Result.CloseAsync(); } catch { /* bỏ qua */ }
                            t.Result.Dispose();
                        }
                        else
                        {
                            _ = t.Exception; // đã quan sát lỗi, tránh cảnh báo task không được xử lý
                        }
                    },
                    TaskScheduler.Default);
                throw new TimeoutException($"quá {_options.ConnectTimeoutMs} ms chưa kết nối được");
            }

            _session = await connectTask;
            _sessionFaulted = false;
            _session.KeepAlive += OnKeepAlive;
            _logger.LogInformation("Đã kết nối OPC UA tới {Endpoint}", _options.EndpointUrl);
            return _session;
        }

        private void OnKeepAlive(ISession session, KeepAliveEventArgs e)
        {
            // Mất liên lạc với server: đánh dấu để lần đọc kế tiếp bỏ phiên và kết nối lại (subscription không tự báo lỗi).
            if (ServiceResult.IsBad(e.Status))
            {
                _sessionFaulted = true;
            }
        }

        /// <summary>
        /// Chế độ subscription: đảm bảo mọi tag được yêu cầu đã có MonitoredItem, đợi tối đa
        /// <see cref="OpcUaOptions.FirstValueWaitMs"/> cho giá trị đầu tiên của tag mới rồi trả giá trị mới nhất trong bộ nhớ.
        /// </summary>
        private async Task<IReadOnlyDictionary<string, PlcTagValue>> ReadSubscribedAsync(
            ISession session, IReadOnlyList<string> ids, CancellationToken cancellationToken)
        {
            if (_sessionFaulted)
            {
                throw new InvalidOperationException("mất liên lạc với Kepware (keep-alive thất bại)");
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.OperationTimeoutMs);
            await EnsureSubscribedAsync(session, ids, timeout.Token);

            var wait = Stopwatch.StartNew();
            while (wait.ElapsedMilliseconds < _options.FirstValueWaitMs && ids.Any(t => !_latest.ContainsKey(t)))
            {
                await Task.Delay(50, cancellationToken);
            }

            var result = new Dictionary<string, PlcTagValue>();
            foreach (var tag in ids)
            {
                result[tag] = _latest.TryGetValue(tag, out var value)
                    ? value
                    : new PlcTagValue(tag, null, false, null, "Chưa nhận được giá trị từ Kepware");
            }

            return result;
        }

        private async Task EnsureSubscribedAsync(ISession session, IReadOnlyList<string> ids, CancellationToken cancellationToken)
        {
            var missing = ids.Where(t => !_items.ContainsKey(t)).Distinct().ToList();
            if (_subscription is not null && missing.Count == 0)
            {
                return;
            }

            if (_subscription is null)
            {
                _subscription = new Subscription(session.DefaultSubscription)
                {
                    PublishingInterval = _options.SamplingIntervalMs,
                    KeepAliveCount = 20,
                    LifetimeCount = 120,
                    MaxNotificationsPerPublish = 200,
                    PublishingEnabled = true,
                    Priority = 100
                };
                session.AddSubscription(_subscription);
                await _subscription.CreateAsync(cancellationToken);
            }

            var created = new List<MonitoredItem>();
            foreach (var tag in missing)
            {
                var item = new MonitoredItem(_subscription.DefaultItem)
                {
                    StartNodeId = new NodeId(tag, (ushort)_options.NamespaceIndex),
                    AttributeId = Attributes.Value,
                    DisplayName = tag,
                    SamplingInterval = _options.SamplingIntervalMs,
                    QueueSize = 1,
                    DiscardOldest = true,
                    Handle = tag
                };
                item.Notification += OnItemNotification;
                _items[tag] = item;
                created.Add(item);
            }

            if (created.Count == 0)
            {
                return;
            }

            _subscription.AddItems(created);
            await _subscription.ApplyChangesAsync(cancellationToken);

            // Tag không tồn tại / bị từ chối: ghi nhận ngay là không tốt thay vì đợi giá trị không bao giờ tới.
            foreach (var item in created)
            {
                if (item.Handle is not string tag)
                {
                    continue;
                }

                if (item.Status?.Error is { } error && ServiceResult.IsBad(error))
                {
                    _latest[tag] = new PlcTagValue(tag, null, false, null, error.StatusCode.ToString());
                }
            }
        }

        private void OnItemNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            if (item.Handle is not string tag)
            {
                return;
            }

            foreach (var value in item.DequeueValues())
            {
                _latest[tag] = ToTagValue(tag, value);
            }
        }

        private async Task<ISession> ConnectAsync(CancellationToken cancellationToken)
        {
            var configuration = _configuration ??= await CreateConfigurationAsync();
            var useSecurity = _options.UseSecurity;

            var endpoint = await CoreClientUtils.SelectEndpointAsync(
                configuration, _options.EndpointUrl, useSecurity, _options.OperationTimeoutMs, Telemetry, cancellationToken);
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);

            IUserIdentity identity = string.IsNullOrWhiteSpace(_options.Username)
                ? new UserIdentity(new AnonymousIdentityToken())
                : new UserIdentity(_options.Username, System.Text.Encoding.UTF8.GetBytes(_options.Password ?? string.Empty));

            return await new DefaultSessionFactory(Telemetry).CreateAsync(
                configuration,
                configuredEndpoint,
                updateBeforeConnect: false,
                sessionName: _options.ApplicationName,
                sessionTimeout: (uint)_options.SessionTimeoutMs,
                identity,
                preferredLocales: null,
                cancellationToken);
        }

        private async Task<ApplicationConfiguration> CreateConfigurationAsync()
        {
            var pki = Path.Combine(AppContext.BaseDirectory, "pki");
            var configuration = new ApplicationConfiguration
            {
                ApplicationName = _options.ApplicationName,
                ApplicationType = ApplicationType.Client,
                ApplicationUri = $"urn:{Utils.GetHostName()}:{_options.ApplicationName.Replace(" ", string.Empty)}",
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(pki, "own"),
                        SubjectName = $"CN={_options.ApplicationName}, O=DRB Vietnam"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(pki, "trusted")
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(pki, "issuer")
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = Path.Combine(pki, "rejected")
                    },
                    AutoAcceptUntrustedCertificates = _options.AutoAcceptUntrustedCertificates,
                    AddAppCertToTrustedStore = true
                },
                TransportQuotas = new TransportQuotas { OperationTimeout = _options.OperationTimeoutMs },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = _options.SessionTimeoutMs },
                TransportConfigurations = new TransportConfigurationCollection()
            };

            await configuration.ValidateAsync(ApplicationType.Client);

            if (_options.AutoAcceptUntrustedCertificates)
            {
                configuration.CertificateValidator.CertificateValidation += (_, e) =>
                {
                    if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
                    {
                        e.Accept = true;
                    }
                };
            }

            // Chỉ khi dùng bảo mật mới cần chứng chỉ ứng dụng; chế độ None (mặc định) bỏ qua.
            if (_options.UseSecurity)
            {
                var application = new ApplicationInstance(Telemetry) { ApplicationName = _options.ApplicationName, ApplicationType = ApplicationType.Client, ApplicationConfiguration = configuration };
                await application.CheckApplicationInstanceCertificatesAsync(false, 0);
            }

            return configuration;
        }

        private async Task DropSessionAsync()
        {
            var session = _session;
            _session = null;

            // Giá trị cũ của phiên đã bỏ không được trả lại như thể còn mới; phiên mới sẽ đăng ký lại từ đầu.
            _subscription = null;
            _items.Clear();
            _latest.Clear();
            _sessionFaulted = false;

            if (session is null)
            {
                return;
            }

            session.KeepAlive -= OnKeepAlive;
            try
            {
                await session.CloseAsync();
            }
            catch
            {
                // phiên đã hỏng thì bỏ qua
            }
            finally
            {
                session.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DropSessionAsync();
            _gate.Dispose();
        }
    }
}

