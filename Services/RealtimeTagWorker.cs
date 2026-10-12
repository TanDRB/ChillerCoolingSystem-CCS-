using System.Globalization;
using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Hubs;
using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Services
{
    public class RealtimeTagWorker : BackgroundService
    {
        private const string MissingValueText = "--";
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

        private readonly IPlcDataReader _reader;
        private readonly IHubContext<ChillerRealtimeHub> _hubContext;
        private readonly MachineLatestValuesStore _store;
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;
        private readonly ILogger<RealtimeTagWorker> _logger;
        private bool _connected = true;

        public RealtimeTagWorker(
            IPlcDataReader reader,
            IHubContext<ChillerRealtimeHub> hubContext,
            MachineLatestValuesStore store,
            IDbContextFactory<HistoryDbContext> dbFactory,
            ILogger<RealtimeTagWorker> logger)
        {
            _reader = reader;
            _hubContext = hubContext;
            _store = store;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tags = await LoadTagsAsync(stoppingToken);
            var tagIds = tags.Select(t => t.TagId).ToList();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PublishAsync(tags, tagIds, stoppingToken);
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RealtimeTagWorker: lỗi vòng lặp chính");
                    await Task.Delay(RetryInterval, stoppingToken);
                }
            }
        }

        private async Task<List<(string Key, string TagId)>> LoadTagsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                    var machines = await db.Machines.Include(m => m.Parameters).AsNoTracking().ToListAsync(cancellationToken);
                    var tags = machines
                        .SelectMany(m => m.Parameters.Select(p => (Key: m.Key + p.ParameterKey, TagId: $"{m.OpcDevice}.{p.ParameterKey}")))
                        .ToList();
                    if (tags.Count > 0)
                    {
                        return tags;
                    }
                    _logger.LogWarning("RealtimeTagWorker: bảng Machine/MachineParameter rỗng, thử lại sau {Sec}s", RetryInterval.TotalSeconds);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "RealtimeTagWorker: nạp danh mục máy từ DB thất bại, thử lại sau {Sec}s", RetryInterval.TotalSeconds);
                }
                await Task.Delay(RetryInterval, cancellationToken);
            }
            return new List<(string Key, string TagId)>();
        }

        private async Task PublishAsync(List<(string Key, string TagId)> tags, List<string> tagIds, CancellationToken cancellationToken)
        {
            var changed = false;
            try
            {
                var values = await _reader.ReadAsync(tagIds, cancellationToken);
                foreach (var (key, tagId) in tags)
                {
                    var value = values[tagId];
                    var good = value.IsGood && value.Value.HasValue;
                    var text = good ? value.Value!.Value.ToString(CultureInfo.InvariantCulture) : MissingValueText;
                    changed |= _store.Set(key, good, text);
                }
                if (!_connected)
                {
                    _connected = true;
                    _logger.LogInformation("RealtimeTagWorker: đã kết nối lại Kepware");
                }
            }
            catch (PlcConnectionException ex)
            {
                if (_connected)
                {
                    _connected = false;
                    _logger.LogWarning("RealtimeTagWorker: mất kết nối Kepware: {Message}", ex.Message);
                }
                foreach (var (key, _) in tags)
                {
                    changed |= _store.Set(key, false, MissingValueText);
                }
            }
            if (changed)
            {
                await _hubContext.Clients.All.SendAsync("MachineUpdate", _store.Snapshot(), cancellationToken);
            }
        }
    }
}
