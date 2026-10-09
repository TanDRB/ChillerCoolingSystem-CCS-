using System.Collections.Concurrent;
using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public interface IExtruderMonitoringService
    {
        IReadOnlyList<ExtruderConfig> GetExtruders();
        ExtruderConfig? FindExtruder(string key);

        /// <summary>Đọc PLC (có cache rất ngắn) và đánh giá từng điểm đo. null nếu không có máy với mã này.</summary>
        Task<ExtruderSnapshot?> GetSnapshotAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Singleton: nhiều trình duyệt cùng polling một máy sẽ dùng chung một lần đọc PLC trong khoảng
    /// <see cref="CacheDuration"/>, tránh dồn tải lên Kepware.
    /// </summary>
    public class ExtruderMonitoringService : IExtruderMonitoringService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMilliseconds(700);

        private readonly IPlcDataReader _reader;
        private readonly IReadOnlyList<ExtruderConfig> _extruders;
        private readonly ILogger<ExtruderMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, (DateTime At, ExtruderSnapshot Snapshot)> _cache = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public ExtruderMonitoringService(
            IPlcDataReader reader,
            IOptions<MonitoringOptions> options,
            ILogger<ExtruderMonitoringService> logger)
        {
            _reader = reader;
            _extruders = options.Value.Extruders;
            _logger = logger;
        }

        public IReadOnlyList<ExtruderConfig> GetExtruders() => _extruders;

        public ExtruderConfig? FindExtruder(string key) =>
            _extruders.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

        public async Task<ExtruderSnapshot?> GetSnapshotAsync(string key, CancellationToken cancellationToken = default)
        {
            var extruder = FindExtruder(key);
            if (extruder is null)
            {
                return null;
            }

            if (TryGetFresh(extruder.Key, out var cached))
            {
                return cached;
            }

            var gate = _locks.GetOrAdd(extruder.Key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (TryGetFresh(extruder.Key, out cached))
                {
                    return cached; // một request khác vừa đọc xong trong lúc mình chờ
                }

                var snapshot = await ReadAsync(extruder, cancellationToken);
                _cache[extruder.Key] = (DateTime.UtcNow, snapshot);
                return snapshot;
            }
            finally
            {
                gate.Release();
            }
        }

        private bool TryGetFresh(string key, out ExtruderSnapshot snapshot)
        {
            if (_cache.TryGetValue(key, out var entry) && DateTime.UtcNow - entry.At < CacheDuration)
            {
                snapshot = entry.Snapshot;
                return true;
            }

            snapshot = null!;
            return false;
        }

        private async Task<ExtruderSnapshot> ReadAsync(ExtruderConfig extruder, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, PlcTagValue> values;
            try
            {
                values = await _reader.ReadAsync(extruder.AllTags.Distinct().ToList(), cancellationToken);
            }
            catch (PlcConnectionException ex)
            {
                _logger.LogWarning(ex, "Không đọc được dữ liệu PLC cho {Extruder}", extruder.DisplayName);
                return Build(extruder, new Dictionary<string, PlcTagValue>(), connectionOk: false, message: ex.Message);
            }

            return Build(extruder, values, connectionOk: true, message: null);
        }

        private static ExtruderSnapshot Build(
            ExtruderConfig extruder,
            IReadOnlyDictionary<string, PlcTagValue> values,
            bool connectionOk,
            string? message)
        {
            PlcTagValue? Get(string? tag) =>
                tag is not null && values.TryGetValue(tag, out var v) ? v : null;

            return new ExtruderSnapshot
            {
                Key = extruder.Key,
                DisplayName = extruder.DisplayName,
                Name = extruder.Name,
                Model = extruder.Model,
                Number = extruder.Number,
                RunState = TemperatureEvaluator.EvaluateRunState(Get(extruder.StartStopTag)),
                ConnectionOk = connectionOk,
                Message = message,
                ReadAt = DateTime.Now,
                Points = extruder.Points
                    .Select(p => TemperatureEvaluator.Evaluate(p, Get(p.Tag)))
                    .ToList()
            };
        }
    }
}

