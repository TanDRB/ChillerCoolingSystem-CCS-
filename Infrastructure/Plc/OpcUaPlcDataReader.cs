using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using ISession = Opc.Ua.Client.ISession;
using StatusCodes = Opc.Ua.StatusCodes;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    // Dùng subscription (như OPC Quick Client) thay vì đọc đồng bộ: tag của thiết bị chậm
    // (vd. MHE TOUCH mất ~9 giây mỗi lần đọc) không còn chặn các tag khác.
    public sealed class OpcUaPlcDataReader : IPlcDataReader, IAsyncDisposable
    {
        private static readonly ITelemetryContext Telemetry = DefaultTelemetry.Create(_ => { });
        private readonly OpcUaOptions _options;
        private readonly ILogger<OpcUaPlcDataReader> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<string, MonitoredItem> _items = new();
        private readonly ConcurrentDictionary<string, PlcTagValue> _latest = new();
        private readonly HashSet<string> _noValueTags = new();
        private ApplicationConfiguration? _configuration;
        private ISession? _session;
        private Subscription? _subscription;
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
            if (tagIds.Count == 0)
            {
                return new Dictionary<string, PlcTagValue>();
            }
            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (_session is not { Connected: true }
                    && _lastFailureMessage is not null
                    && DateTime.UtcNow - _lastFailureAt < TimeSpan.FromMilliseconds(_options.RetryCooldownMs))
                {
                    throw new PlcConnectionException(_lastFailureMessage);
                }
                var session = await EnsureSessionAsync(cancellationToken);
                if (_sessionFaulted)
                {
                    throw new InvalidOperationException("mất liên lạc với Kepware (keep-alive thất bại)");
                }
                var ids = tagIds.Distinct().ToList();
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeout.CancelAfter(_options.OperationTimeoutMs);
                    await EnsureSubscribedAsync(session, ids, timeout.Token);
                }
                await WaitForFirstValuesAsync(ids, cancellationToken);
                _lastFailureMessage = null;
                return ids.ToDictionary(
                    tag => tag,
                    tag => _latest.TryGetValue(tag, out var value)
                        ? value
                        : new PlcTagValue(tag, null, false, null, "Chưa nhận được giá trị từ Kepware"));
            }
            catch (PlcConnectionException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await DropSessionAsync();
                var reason = ex is OperationCanceledException ? "quá thời gian chờ" : ex.Message;
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
                // float32 -> chuỗi ngắn nhất để 1.9f không thành 1.899999976...
                var number = data.Value is float single
                    ? double.Parse(single.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                    : Convert.ToDouble(data.Value, CultureInfo.InvariantCulture);
                var good = StatusCode.IsGood(data.StatusCode);
                var timestamp = data.SourceTimestamp == DateTime.MinValue ? (DateTime?)null : data.SourceTimestamp.ToLocalTime();
                return new PlcTagValue(tag, number, good, timestamp, good ? null : data.StatusCode.ToString());
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                return new PlcTagValue(tag, null, false, null, "Giá trị không phải kiểu số");
            }
        }

        private async Task EnsureSubscribedAsync(ISession session, List<string> ids, CancellationToken cancellationToken)
        {
            var missing = ids.Where(tag => !_items.ContainsKey(tag)).ToList();
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
            foreach (var item in created)
            {
                if (item.Handle is string tag && item.Status?.Error is { } error && ServiceResult.IsBad(error))
                {
                    _latest[tag] = new PlcTagValue(tag, null, false, null, error.StatusCode.ToString());
                }
            }
        }

        private async Task WaitForFirstValuesAsync(List<string> ids, CancellationToken cancellationToken)
        {
            var waiting = ids.Where(tag => !_latest.ContainsKey(tag) && !_noValueTags.Contains(tag)).ToList();
            if (waiting.Count == 0)
            {
                return;
            }
            var wait = Stopwatch.StartNew();
            while (wait.ElapsedMilliseconds < _options.FirstValueWaitMs && waiting.Any(tag => !_latest.ContainsKey(tag)))
            {
                await Task.Delay(50, cancellationToken);
            }
            foreach (var tag in waiting.Where(tag => !_latest.ContainsKey(tag)))
            {
                _noValueTags.Add(tag);
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

        private void OnKeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
            {
                _sessionFaulted = true;
            }
        }

        private async Task<ISession> EnsureSessionAsync(CancellationToken cancellationToken)
        {
            if (_session is { Connected: true })
            {
                return _session;
            }
            await DropSessionAsync();
            var connectTask = ConnectAsync(cancellationToken);
            var timeout = Task.Delay(_options.ConnectTimeoutMs, cancellationToken);
            if (await Task.WhenAny(connectTask, timeout) != connectTask)
            {
                _ = connectTask.ContinueWith(
                    async task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            try { await task.Result.CloseAsync(); } catch { }
                            task.Result.Dispose();
                        }
                        else
                        {
                            _ = task.Exception;
                        }
                    },
                    TaskScheduler.Default);
                throw new TimeoutException($"quá {_options.ConnectTimeoutMs} ms chưa kết nối được");
            }
            _session = await connectTask;
            _session.KeepAlive += OnKeepAlive;
            _logger.LogInformation("Đã kết nối OPC UA tới {Endpoint}", _options.EndpointUrl);
            return _session;
        }

        private async Task<ISession> ConnectAsync(CancellationToken cancellationToken)
        {
            var configuration = _configuration ??= await CreateConfigurationAsync();
            var endpoint = await CoreClientUtils.SelectEndpointAsync(
                configuration, _options.EndpointUrl, _options.UseSecurity, _options.OperationTimeoutMs, Telemetry, cancellationToken);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(configuration));
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
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "trusted") },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "issuer") },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "rejected") },
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
            if (_options.UseSecurity)
            {
                var application = new ApplicationInstance(Telemetry)
                {
                    ApplicationName = _options.ApplicationName,
                    ApplicationType = ApplicationType.Client,
                    ApplicationConfiguration = configuration
                };
                await application.CheckApplicationInstanceCertificatesAsync(false, 0);
            }
            return configuration;
        }

        private async Task DropSessionAsync()
        {
            var session = _session;
            _session = null;
            _subscription = null;
            _items.Clear();
            _latest.Clear();
            _noValueTags.Clear();
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
