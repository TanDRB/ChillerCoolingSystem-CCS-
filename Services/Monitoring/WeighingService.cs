using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Một lần cân đã ghi nhận.</summary>
    public sealed record WeighingEntry(double WeightKg, DateTime RecordedAt);

    public sealed class WeighingSnapshot
    {
        public string Key { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string Name { get; init; } = "";
        public string Model { get; init; } = "";
        public string Number { get; init; } = "";
        public bool ConnectionOk { get; init; }

        /// <summary>false = trạm chưa được gắn tag trọng lượng (đã dựng sẵn giao diện, chờ có dữ liệu).</summary>
        public bool Configured { get; init; } = true;

        /// <summary>
        /// false = đã đọc được từ Kepware nhưng tag chưa có giá trị tốt (Quality Bad/Unknown, vd. PLC chưa có
        /// chương trình hoặc tag chưa được gán). Khác với mất kết nối tới Kepware.
        /// </summary>
        public bool TagOk { get; init; }

        public string? Message { get; init; }
        public DateTime ReadAt { get; init; }

        /// <summary>Giá trị trọng lượng đang đọc trực tiếp từ PLC (null nếu chưa đọc được).</summary>
        public double? LiveWeight { get; init; }

        /// <summary>Chiều dài thực tế (mm); null nếu chưa có tag/dữ liệu.</summary>
        public double? LengthMm { get; init; }

        /// <summary>3 lần cân gần nhất, mới nhất trước (N1, N2, N3); thiếu thì null.</summary>
        public IReadOnlyList<WeighingEntry?> Weights { get; init; } = new WeighingEntry?[3];
    }

    public interface IWeighingService
    {
        IReadOnlyList<WeightStationConfig> GetStations();
        WeightStationConfig? FindStation(string key);
        WeighingSnapshot? GetSnapshot(string key);
    }

    /// <summary>
    /// Chạy nền (không phụ thuộc có ai mở trang hay không) để không bỏ sót lần cân nào: đọc tag trọng lượng
    /// mỗi 0,5 giây. Một lần cân là một chu kỳ "tấm cao su đặt lên cân -> nhấc ra" (giá trị &gt; 0, kết thúc khi
    /// về 0): giá trị tăng dần, đứng yên một đoạn rồi giảm. Khi chu kỳ kết thúc, lấy ĐOẠN ỔN ĐỊNH DÀI NHẤT
    /// làm kết quả lần cân (bỏ qua đoạn tăng/giảm lúc đặt/nhấc) và lưu vào DB.
    /// </summary>
    public sealed class WeighingService : BackgroundService, IWeighingService
    {
        private const int MinPlateauReads = 3;   // đoạn ổn định tối thiểu 3 lần đọc (1,5 giây)
        private const int ZeroReadsToFinish = 2; // về 0 liên tục 2 lần đọc (1 giây) mới coi là đã nhấc ra
        private const int MaxSamples = 600;      // chặn bộ nhớ nếu giá trị > 0 kéo dài bất thường (5 phút)
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

        private readonly IPlcDataReader _reader;
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;
        private readonly IReadOnlyList<WeightStationConfig> _stations;
        private readonly ILogger<WeighingService> _logger;
        private readonly Dictionary<string, StationState> _states = new();

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

            foreach (var station in _stations)
            {
                _states[station.Key] = new StationState();
            }
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

            lock (_states[station.Key])
            {
                return BuildSnapshot(station, _states[station.Key]);
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
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WeighingService: lỗi không mong đợi khi đọc trạm cân");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
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
                        .Take(3)
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
            var tags = _stations.SelectMany(s => s.AllTags).Distinct().ToList();

            IReadOnlyDictionary<string, PlcTagValue> values;
            try
            {
                values = await _reader.ReadAsync(tags, cancellationToken);
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

            foreach (var station in _stations)
            {
                if (string.IsNullOrWhiteSpace(station.WeightTag))
                {
                    continue; // trạm chưa có tag: chỉ hiển thị giao diện, không đọc gì
                }

                values.TryGetValue(station.WeightTag, out var weight);
                PlcTagValue? length = null;
                if (!string.IsNullOrWhiteSpace(station.LengthTag))
                {
                    values.TryGetValue(station.LengthTag, out length);
                }

                WeighingEntry? toSave = null;
                var state = _states[station.Key];
                lock (state)
                {
                    state.ConnectionOk = true;
                    state.Message = null;
                    state.ReadAt = DateTime.Now;
                    state.LengthMm = length is { IsGood: true, Value: not null } ? length.Value : null;

                    if (weight is { IsGood: true, Value: not null })
                    {
                        state.TagOk = true;
                        state.LiveRaw = weight.Value;
                        toSave = Track(station, state, weight.Value.Value);
                    }
                    else
                    {
                        state.TagOk = false;
                        state.LiveRaw = null; // tag lỗi: không đổi trạng thái nhận diện lần cân
                    }
                }

                if (toSave is not null)
                {
                    await SaveAsync(station, toSave, cancellationToken);
                }
            }
        }

        /// <summary>Cập nhật bộ nhận diện lần cân; trả về lần cân mới nếu vừa phát hiện (đã chèn vào History).</summary>
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

            if (state.Samples.Count == 0)
            {
                return null; // bàn cân đang trống
            }

            if (++state.ZeroReads < ZeroReadsToFinish)
            {
                return null; // có thể chỉ là nhiễu thoáng qua, chờ thêm
            }

            // Đã nhấc tấm cao su ra: chốt kết quả của chu kỳ vừa rồi.
            var plateau = FindPlateau(state.Samples, station.PlateauTolerance);
            state.Samples.Clear();
            state.ZeroReads = 0;
            if (plateau is null)
            {
                return null; // chu kỳ quá ngắn/nhiễu, không có đoạn ổn định nào đủ dài
            }

            var entry = new WeighingEntry(Math.Round(plateau.Value * station.Scale, 1), DateTime.Now);
            state.History.Insert(0, entry);
            if (state.History.Count > 3)
            {
                state.History.RemoveRange(3, state.History.Count - 3);
            }

            return entry;
        }

        /// <summary>
        /// Tìm đoạn đọc liên tiếp dài nhất mà mọi giá trị chỉ lệch tối đa <paramref name="tolerance"/> so với giá trị
        /// đầu đoạn (khi hai đoạn dài bằng nhau thì lấy đoạn về sau), trả về trung vị của đoạn đó; null nếu không đoạn
        /// nào dài tới <see cref="MinPlateauReads"/> lần đọc.
        /// </summary>
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

                // Nếu hai bản app cùng chạy trên một DB (vd. máy chủ + bản debug) thì cả hai cùng thấy lần cân này:
                // bỏ qua khi DB đã có lần cân của trạm trong vài giây gần đây để không ghi trùng.
                var recordedAtUtc = entry.RecordedAt.ToUniversalTime();
                var since = recordedAtUtc.AddSeconds(-10);
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
                // Vẫn giữ trong bộ nhớ để hiển thị; chỉ mất dữ liệu khi khởi động lại.
                _logger.LogWarning(ex, "WeighingService: không lưu được lần cân {Weight} kg vào DB", entry.WeightKg);
            }
        }

        private static WeighingSnapshot BuildSnapshot(WeightStationConfig station, StationState state)
        {
            var weights = new WeighingEntry?[3];
            var offset = 0;
            if (state.Samples.Count > 0)
            {
                // Đang có tấm cao su trên cân: N1 là giá trị hiện tại (đang đọc từ PLC, hoặc đoạn ổn định tạm
                // thời nếu vừa nhấc ra); N2, N3 là 2 lần cân hoàn tất gần nhất.
                var current = state.LiveRaw is > 0 && state.LiveRaw >= station.MinValue
                    ? state.LiveRaw
                    : FindPlateau(state.Samples, station.PlateauTolerance) ?? state.Samples[^1];
                weights[0] = new WeighingEntry(Math.Round(current!.Value * station.Scale, 1), state.ReadAt);
                offset = 1;
            }

            for (var i = offset; i < 3 && i - offset < state.History.Count; i++)
            {
                weights[i] = state.History[i - offset];
            }

            return new WeighingSnapshot
            {
                Key = station.Key,
                DisplayName = station.DisplayName,
                Name = station.Name,
                Model = station.Model,
                Number = station.Number,
                ConnectionOk = state.ConnectionOk,
                Configured = !string.IsNullOrWhiteSpace(station.WeightTag),
                TagOk = state.TagOk,
                Message = state.Message,
                ReadAt = state.ReadAt,
                LiveWeight = state.LiveRaw is null ? null : Math.Round(state.LiveRaw.Value * station.Scale, 1),
                LengthMm = state.LengthMm,
                Weights = weights
            };
        }

        private sealed class StationState
        {
            public List<WeighingEntry> History { get; } = new();
            /// <summary>Các lần đọc (giá trị thô, &gt; 0) của chu kỳ cân đang diễn ra; rỗng = bàn cân trống.</summary>
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
