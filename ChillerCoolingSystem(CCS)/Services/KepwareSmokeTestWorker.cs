using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Globalization;

namespace ChillerCoolingSystem_CCS_.Services
{
    public class KepwareSmokeTestWorker : BackgroundService
    {
        private readonly string _endpointUrl = "opc.tcp://192.168.37.150:49320";

        private readonly IHubContext<ChillerRealtimeHub> _hubContext;
        private readonly MachineLatestValuesStore _store;

        // Worker là singleton nên không thể inject thẳng IMachineRepository (Scoped).
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;
        private readonly ILogger<KepwareSmokeTestWorker> _logger;

        private ApplicationConfiguration? _config;
        private Session? _session;
        private Subscription? _subscription;
        private bool _hasBrowsedOnce;

        // Nạp từ bảng Machine/MachineParameter 1 lần lúc khởi động, retry nếu DB chưa sẵn sàng.
        private List<(string Key, string Device, string TagName)> _tagMap = new();
        private bool _tagMapLoaded;
        private DateTime _lastTagMapLoadFailUtc = DateTime.MinValue;

        private DateTime _lastConnectFailUtc = DateTime.MinValue;
        private readonly TimeSpan _connectRetryInterval = TimeSpan.FromSeconds(5);

        public KepwareSmokeTestWorker(
            IHubContext<ChillerRealtimeHub> hubContext,
            MachineLatestValuesStore store,
            IDbContextFactory<HistoryDbContext> dbFactory,
            ILogger<KepwareSmokeTestWorker> logger)
        {
            _hubContext = hubContext;
            _store = store;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_tagMapLoaded)
                    {
                        await EnsureTagMapLoadedAsync();
                    }

                    if (_session == null || !_session.Connected)
                    {
                        await EnsureSessionAsync();
                    }

                    if (_session != null && _session.Connected && !_hasBrowsedOnce)
                    {
                        BrowseObjectsFolderForDiagnostics();
                        _hasBrowsedOnce = true;
                    }

                    if (_tagMapLoaded && _session != null && _session.Connected && _subscription == null)
                    {
                        CreateSubscription();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "KepwareSmokeTestWorker: lỗi vòng lặp chính");
                    ResetOpc();
                    _lastConnectFailUtc = DateTime.UtcNow;
                }

                await Task.Delay(1000, stoppingToken);
            }

            ResetOpc();
        }

        private async Task EnsureTagMapLoadedAsync()
        {
            if (DateTime.UtcNow - _lastTagMapLoadFailUtc < _connectRetryInterval)
            {
                return;
            }

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var machines = await db.Machines
                    .Include(m => m.Parameters)
                    .AsNoTracking()
                    .ToListAsync();

                _tagMap = machines
                    .SelectMany(m => m.Parameters.Select(p =>
                        (Key: m.Key + p.ParameterKey, Device: m.OpcDevice, TagName: p.ParameterKey)))
                    .ToList();

                _tagMapLoaded = _tagMap.Count > 0;

                if (!_tagMapLoaded)
                {
                    _logger.LogWarning("KepwareSmokeTestWorker: bảng Machine/MachineParameter rỗng, thử lại sau {Sec}s", _connectRetryInterval.TotalSeconds);
                    _lastTagMapLoadFailUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _lastTagMapLoadFailUtc = DateTime.UtcNow;
                _logger.LogWarning(ex, "KepwareSmokeTestWorker: nạp danh mục máy từ DB thất bại, thử lại sau {Sec}s", _connectRetryInterval.TotalSeconds);
            }
        }

        private async Task EnsureSessionAsync()
        {
            if (DateTime.UtcNow - _lastConnectFailUtc < _connectRetryInterval)
            {
                return;
            }

            ResetOpc();

            _config = new ApplicationConfiguration
            {
                ApplicationName = "ChillerCoolingSystem_CCS",
                ApplicationUri = "urn:localhost:ChillerCoolingSystem_CCS",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "ChillerCoolingSystem_CCS"
                    },

                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Applications"
                    },

                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities"
                    },

                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/RejectedCertificates"
                    },

                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true
                },

                TransportConfigurations = new TransportConfigurationCollection(),

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 8000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 600000,
                    SecurityTokenLifetime = 3600000
                },

                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000,
                    MinSubscriptionLifetime = 10000
                }
            };

            await _config.Validate(ApplicationType.Client);

            _config.CertificateValidator.CertificateValidation += (sender, e) =>
            {
                e.Accept = true;
            };

            try
            {
                _logger.LogInformation("KepwareSmokeTestWorker: đang kết nối tới {Url} ...", _endpointUrl);

                var endpointDescription = CoreClientUtils.SelectEndpoint(_config, _endpointUrl, false);
                var endpointConfiguration = EndpointConfiguration.Create(_config);
                var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                _session = await Session.Create(
                    _config,
                    endpoint,
                    false,
                    "CCS_KEPWARE_SMOKE_TEST_SESSION",
                    30000,
                    null,
                    null);

                _lastConnectFailUtc = DateTime.MinValue;
                _logger.LogInformation("KepwareSmokeTestWorker: đã kết nối OPC UA tới {Url}", _endpointUrl);
            }
            catch (Exception ex)
            {
                _lastConnectFailUtc = DateTime.UtcNow;
                _logger.LogWarning(
                    "KepwareSmokeTestWorker: kết nối thất bại, thử lại sau {Sec}s: {Msg}",
                    _connectRetryInterval.TotalSeconds, ex.Message);
                ResetOpc();
            }
        }
        private void BrowseObjectsFolderForDiagnostics()
        {
            try
            {
                var browser = new Browser(_session)
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = 0
                };

                var results = browser.Browse(ObjectIds.ObjectsFolder);

                _logger.LogInformation(
                    "KepwareSmokeTestWorker: Objects folder có {Count} node con (chỉ log 1 lần lúc khởi động).",
                    results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "KepwareSmokeTestWorker: browse Objects folder thất bại (không chặn worker)");
            }
        }

        private void CreateSubscription()
        {
            if (_session == null || !_session.Connected)
            {
                return;
            }

            try
            {
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 100,
                    KeepAliveCount = 100,
                    LifetimeCount = 600,
                    MaxNotificationsPerPublish = 100,
                    PublishingEnabled = true,
                    Priority = 100
                };

                _session.AddSubscription(_subscription);
                _subscription.Create();

                var items = new List<MonitoredItem>();

                foreach (var (key, device, tagName) in _tagMap)
                {
                    var nodeId = $"ns=2;s={device}.{tagName}";

                    var item = new MonitoredItem(_subscription.DefaultItem)
                    {
                        StartNodeId = NodeId.Parse(nodeId),
                        AttributeId = Attributes.Value,
                        DisplayName = $"{device}.{tagName}",
                        SamplingInterval = 100,
                        QueueSize = 1,
                        DiscardOldest = true,
                        Handle = key
                    };

                    item.Notification += OnTagNotification;
                    items.Add(item);
                }

                _subscription.AddItems(items);
                _subscription.ApplyChanges();

                _logger.LogInformation(
                    "KepwareSmokeTestWorker: đã tạo subscription cho {Count} tag ({Devices})",
                    items.Count, string.Join(", ", _tagMap.Select(t => t.Device).Distinct()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KepwareSmokeTestWorker: tạo subscription thất bại");
                _subscription = null;
            }
        }

        private void OnTagNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            var key = monitoredItem.Handle as string;

            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            foreach (var value in monitoredItem.DequeueValues())
            {
                var good = value != null && !StatusCode.IsBad(value.StatusCode);
                var text = good
                    ? Convert.ToString(value!.Value, CultureInfo.InvariantCulture)
                    : "--";

                _logger.LogInformation(
                    "KepwareSmokeTestWorker: {Key} = {Value} (quality: {Quality})",
                    key, text, good ? "Good" : "Bad");

                _store.Set(key, good, text);

                _ = _hubContext.Clients.All.SendAsync("MachineUpdate", _store.Snapshot());
            }
        }

        private void ResetOpc()
        {
            try
            {
                if (_subscription != null)
                {
                    _subscription.Delete(true);

                    if (_session != null)
                    {
                        _session.RemoveSubscription(_subscription);
                    }
                }
            }
            catch
            {
            }

            try
            {
                _session?.Close();
                _session?.Dispose();
            }
            catch
            {
            }

            _subscription = null;
            _session = null;
        }
    }
}
