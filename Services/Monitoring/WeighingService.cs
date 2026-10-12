using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public sealed record WeighingEntry(double WeightKg, DateTime RecordedAt);

    public sealed class WeighingSnapshot
    {
        public bool ConnectionOk { get; init; }

        public bool Configured { get; init; } = true;

        public bool TagOk { get; init; }

        public string? Message { get; init; }

        public DateTime ReadAt { get; init; }

        public double? LengthMm { get; init; }

        public IReadOnlyList<WeighingEntry?> Weights { get; init; } = new WeighingEntry?[3];
    }

    public interface IWeighingService
    {
        IReadOnlyList<WeightStationConfig> GetStations();

        WeightStationConfig? FindStation(string key);

        WeighingSnapshot? GetSnapshot(string key);
    }

    // Một lần cân = chu kỳ đặt tấm lên cân -> nhấc ra; kết quả là đoạn đọc ổn định dài nhất của chu kỳ đó.
    public sealed class WeighingService : BackgroundService, IWeighingService
    {
        private const int HistorySize = 3;
        private const int MinPlateauReads = 3;
        private const int ZeroReadsToFinish = 2;
        private const int MaxSamples = 600;
        private const int DuplicateWindowSeconds = 10;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

        private readonly IPlcDataReader _reader;
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;
        private readonly IReadOnlyList<WeightStationConfig> _stations;
        private readonly ILogger<WeighingService> _logger;
        private readonly Dictionary<string, StationState> _states;

        public WeighingService(
            IPlcDataReader reader,
            IDbContextFactory<HistoryDbContext> dbFactory,
            IOptions<MonitoringOptions> options,
            ILogger<WeighingService> logger)
        {
            _reader = reader;
            _dbFactory = dbFactory;
            _stations = options.Value.WeightStations;
            _logger = logger;
            _states = _stations.ToDictionary(station => station.Key, _ => new StationState());
        }

        public IReadOnlyList<WeightStationConfig> GetStations() => _stations;

        public WeightStationConfig? FindStation(string key) =>
            _stations.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

        public WeighingSnapshot? GetSnapshot(string key)
        {
            var station = FindStation(key);
            if (station is null)
            {
                return null;
            }
            var state = _states[station.Key];
            lock (state)
            {
                return BuildSnapshot(station, state);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_stations.Count == 0)
            {
                return;
            }
            await LoadHistoryAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollOnceAsync(stoppingToken);
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WeighingService: lỗi không mong đợi khi đọc trạm cân");
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
        }

        private async Task LoadHistoryAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                foreach (var station in _stations)
                {
                    var recent = await db.WeighingRecords
                        .AsNoTracking()
                        .Where(w => w.StationKey == station.Key)
                        .OrderByDescending(w => w.RecordedAtUtc)
                        .Take(HistorySize)
                        .ToListAsync(cancellationToken);
                    var state = _states[station.Key];
                    lock (state)
                    {
                        state.History.AddRange(recent.Select(w =>
                            new WeighingEntry(w.WeightKg, DateTime.SpecifyKind(w.RecordedAtUtc, DateTimeKind.Utc).ToLocalTime())));
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WeighingService: không nạp được lịch sử cân từ DB, bắt đầu từ trống");
            }
        }

        private async Task PollOnceAsync(CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, PlcTagValue> values;
            try
            {
                values = await _reader.ReadAsync(_stations.SelectMany(s => s.AllTags).Distinct().ToList(), cancellationToken);
            }
            catch (PlcConnectionException ex)
            {
                foreach (var station in _stations)
                {
                    var state = _states[station.Key];
                    lock (state)
                    {
                        state.ConnectionOk = false;
                        state.Message = ex.Message;
                        state.ReadAt = DateTime.Now;
                    }
                }
                return;
            }
            foreach (var station in _stations.Where(s => !string.IsNullOrWhiteSpace(s.WeightTag)))
            {
                values.TryGetValue(station.WeightTag, out var weight);
                var length = string.IsNullOrWhiteSpace(station.LengthTag) ? null : values.GetValueOrDefault(station.LengthTag);
                var state = _states[station.Key];
                WeighingEntry? finished = null;
                lock (state)
                {
                    state.ConnectionOk = true;
                    state.Message = null;
                    state.ReadAt = DateTime.Now;
                    state.LengthMm = length is { IsGood: true, Value: not null } ? length.Value : null;
                    state.TagOk = weight is { IsGood: true, Value: not null };
                    state.LiveRaw = state.TagOk ? weight!.Value : null;
                    if (state.TagOk)
                    {
                        finished = Track(station, state, weight!.Value!.Value);
                    }
                }
                if (finished is not null)
                {
                    await SaveAsync(station, finished, cancellationToken);
                }
            }
        }

        private static WeighingEntry? Track(WeightStationConfig station, StationState state, double raw)
        {
            if (raw > 0 && raw >= station.MinValue)
            {
                state.ZeroReads = 0;
                state.Samples.Add(raw);
                if (state.Samples.Count > MaxSamples)
                {
                    state.Samples.RemoveAt(0);
                }
                return null;
            }
            if (state.Samples.Count == 0 || ++state.ZeroReads < ZeroReadsToFinish)
            {
                return null;
            }
            var plateau = FindPlateau(state.Samples, station.PlateauTolerance);
            state.Samples.Clear();
            state.ZeroReads = 0;
            if (plateau is null)
            {
                return null;
            }
            var entry = new WeighingEntry(Math.Round(plateau.Value * station.Scale, 1), DateTime.Now);
            state.History.Insert(0, entry);
            if (state.History.Count > HistorySize)
            {
                state.History.RemoveRange(HistorySize, state.History.Count - HistorySize);
            }
            return entry;
        }

        private static double? FindPlateau(IReadOnlyList<double> samples, double tolerance)
        {
            var bestStart = -1;
            var bestLength = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                var length = 1;
                while (i + length < samples.Count && Math.Abs(samples[i + length] - samples[i]) <= tolerance)
                {
                    length++;
                }
                if (length >= bestLength)
                {
                    bestLength = length;
                    bestStart = i;
                }
            }
            if (bestLength < MinPlateauReads)
            {
                return null;
            }
            var run = samples.Skip(bestStart).Take(bestLength).OrderBy(v => v).ToList();
            return run[run.Count / 2];
        }

        private async Task SaveAsync(WeightStationConfig station, WeighingEntry entry, CancellationToken cancellationToken)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var recordedAtUtc = entry.RecordedAt.ToUniversalTime();
                var since = recordedAtUtc.AddSeconds(-DuplicateWindowSeconds);
                if (await db.WeighingRecords.AnyAsync(w => w.StationKey == station.Key && w.RecordedAtUtc >= since, cancellationToken))
                {
                    return;
                }
                db.WeighingRecords.Add(new WeighingRecord
                {
                    StationKey = station.Key,
                    WeightKg = entry.WeightKg,
                    RecordedAtUtc = recordedAtUtc
                });
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("WeighingService: {Station} ghi nhận lần cân {Weight} kg", station.DisplayName, entry.WeightKg);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WeighingService: không lưu được lần cân {Weight} kg vào DB", entry.WeightKg);
            }
        }

        private static WeighingSnapshot BuildSnapshot(WeightStationConfig station, StationState state)
        {
            var weights = new WeighingEntry?[HistorySize];
            var offset = 0;
            if (state.Samples.Count > 0)
            {
                var current = state.LiveRaw is > 0 && state.LiveRaw >= station.MinValue
                    ? state.LiveRaw
                    : FindPlateau(state.Samples, station.PlateauTolerance) ?? state.Samples[^1];
                weights[0] = new WeighingEntry(Math.Round(current!.Value * station.Scale, 1), state.ReadAt);
                offset = 1;
            }
            for (var i = offset; i < HistorySize && i - offset < state.History.Count; i++)
            {
                weights[i] = state.History[i - offset];
            }
            return new WeighingSnapshot
            {
                ConnectionOk = state.ConnectionOk,
                Configured = !string.IsNullOrWhiteSpace(station.WeightTag),
                TagOk = state.TagOk,
                Message = state.Message,
                ReadAt = state.ReadAt,
                LengthMm = state.LengthMm,
                Weights = weights
            };
        }

        private sealed class StationState
        {
            public List<WeighingEntry> History { get; } = new();

            public List<double> Samples { get; } = new();

            public int ZeroReads;

            public bool ConnectionOk;

            public bool TagOk;

            public string? Message = "Đang kết nối...";

            public DateTime ReadAt = DateTime.Now;

            public double? LiveRaw;

            public double? LengthMm;
        }
    }
}
