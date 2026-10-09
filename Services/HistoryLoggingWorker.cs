using System.Text.RegularExpressions;
using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Services
{
    public partial class HistoryLoggingWorker : BackgroundService
    {
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;
        private readonly MachineLatestValuesStore _store;
        private readonly IConfiguration _config;
        private readonly ILogger<HistoryLoggingWorker> _logger;

        private readonly Dictionary<string, bool> _lastFaultState = new();
        private DateTime _lastCleanupUtc = DateTime.MinValue;

        public HistoryLoggingWorker(
            IDbContextFactory<HistoryDbContext> dbFactory,
            MachineLatestValuesStore store,
            IConfiguration config,
            ILogger<HistoryLoggingWorker> logger)
        {
            _dbFactory = dbFactory;
            _store = store;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _config.GetValue("HistoryLogging:IntervalSeconds", 300);

            await using (var db = await _dbFactory.CreateDbContextAsync(stoppingToken))
            {
                await db.Database.MigrateAsync(stoppingToken);
                await MachineCatalogSeeder.SeedAsync(db, stoppingToken);
                await LoadOpenAlarmsAsync(db, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await LogSnapshotAsync(stoppingToken);
                    await RunRetentionCleanupIfDueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HistoryLoggingWorker: lỗi vòng lặp ghi log");
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task LoadOpenAlarmsAsync(HistoryDbContext db, CancellationToken ct)
        {
            var openAlarms = await db.AlarmEvents
                .Where(a => a.EndUtc == null)
                .Select(a => a.MachineKey)
                .ToListAsync(ct);

            foreach (var machineKey in openAlarms)
            {
                _lastFaultState[machineKey] = true;
            }
        }

        [GeneratedRegex(@"^(?<machine>[a-z]+\d+)(?<tag>[A-Z][a-zA-Z]*)$")]
        private static partial Regex TagKeyRegex();

        private async Task LogSnapshotAsync(CancellationToken ct)
        {
            var snapshot = _store.Snapshot();
            if (snapshot.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var entries = new List<TagHistoryEntry>();
            var faultByMachine = new Dictionary<string, (bool Good, string? Value)>();

            foreach (var (key, tag) in snapshot)
            {
                var match = TagKeyRegex().Match(key);
                if (!match.Success)
                {
                    _logger.LogWarning(
                        "HistoryLoggingWorker: key '{Key}' không khớp dạng machineKey+TagName (vd 'ct3TempOut'), bỏ qua ghi lịch sử",
                        key);
                    continue;
                }

                var machineKey = match.Groups["machine"].Value;
                var tagName = match.Groups["tag"].Value;

                entries.Add(new TagHistoryEntry
                {
                    MachineKey = machineKey,
                    TagName = tagName,
                    Value = tag.Value,
                    Good = tag.Good,
                    TimestampUtc = now
                });

                if (tagName == "Fault")
                {
                    faultByMachine[machineKey] = (tag.Good, tag.Value);
                }
            }

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            db.TagHistoryEntries.AddRange(entries);
            await ProcessFaultTransitionsAsync(db, faultByMachine, now, ct);

            await db.SaveChangesAsync(ct);
        }

        private async Task ProcessFaultTransitionsAsync(
            HistoryDbContext db,
            Dictionary<string, (bool Good, string? Value)> faultByMachine,
            DateTime now,
            CancellationToken ct)
        {
            foreach (var (machineKey, fault) in faultByMachine)
            {
                if (!fault.Good)
                {
                    continue;
                }

                var isFault = fault.Value != "0";
                var wasFault = _lastFaultState.GetValueOrDefault(machineKey);

                if (isFault && !wasFault)
                {
                    db.AlarmEvents.Add(new AlarmEvent { MachineKey = machineKey, StartUtc = now });
                    _lastFaultState[machineKey] = true;
                }
                else if (!isFault && wasFault)
                {
                    var openAlarm = await db.AlarmEvents
                        .Where(a => a.MachineKey == machineKey && a.EndUtc == null)
                        .OrderByDescending(a => a.StartUtc)
                        .FirstOrDefaultAsync(ct);

                    if (openAlarm != null)
                    {
                        openAlarm.EndUtc = now;
                    }

                    _lastFaultState[machineKey] = false;
                }
            }
        }

        private async Task RunRetentionCleanupIfDueAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanupUtc < TimeSpan.FromDays(1))
            {
                return;
            }

            _lastCleanupUtc = now;
            var retentionDays = _config.GetValue("HistoryLogging:RetentionDays", 90);
            var cutoff = now.AddDays(-retentionDays);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var deletedEntries = await db.TagHistoryEntries
                .Where(e => e.TimestampUtc < cutoff)
                .ExecuteDeleteAsync(ct);

            var deletedAlarms = await db.AlarmEvents
                .Where(a => a.EndUtc != null && a.EndUtc < cutoff)
                .ExecuteDeleteAsync(ct);

            if (deletedEntries > 0 || deletedAlarms > 0)
            {
                _logger.LogInformation(
                    "HistoryLoggingWorker: dọn {Entries} dòng history và {Alarms} alarm cũ hơn {Days} ngày",
                    deletedEntries, deletedAlarms, retentionDays);
            }
        }
    }
}
