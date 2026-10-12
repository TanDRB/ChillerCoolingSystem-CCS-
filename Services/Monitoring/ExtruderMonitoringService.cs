using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public interface IExtruderMonitoringService
    {
        IReadOnlyList<ExtruderConfig> GetExtruders();

        ExtruderConfig? FindExtruder(string key);

        Task<ExtruderSnapshot?> GetSnapshotAsync(string key, CancellationToken cancellationToken = default);
    }

    public class ExtruderMonitoringService : IExtruderMonitoringService
    {
        private readonly IPlcDataReader _reader;
        private readonly IReadOnlyList<ExtruderConfig> _extruders;
        private readonly ILogger<ExtruderMonitoringService> _logger;

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
            try
            {
                var values = await _reader.ReadAsync(extruder.AllTags.Distinct().ToList(), cancellationToken);
                return Build(extruder, values, connectionOk: true, message: null);
            }
            catch (PlcConnectionException ex)
            {
                _logger.LogWarning(ex, "Không đọc được dữ liệu PLC cho {Extruder}", extruder.DisplayName);
                return Build(extruder, new Dictionary<string, PlcTagValue>(), connectionOk: false, message: ex.Message);
            }
        }

        private static ExtruderSnapshot Build(
            ExtruderConfig extruder,
            IReadOnlyDictionary<string, PlcTagValue> values,
            bool connectionOk,
            string? message)
        {
            PlcTagValue? Get(string? tag) => tag is not null && values.TryGetValue(tag, out var value) ? value : null;
            return new ExtruderSnapshot
            {
                RunState = TemperatureEvaluator.EvaluateRunState(Get(extruder.StartStopTag)),
                ConnectionOk = connectionOk,
                Message = message,
                ReadAt = DateTime.Now,
                Points = extruder.Points.Select(p => TemperatureEvaluator.Evaluate(p, Get(p.Tag))).ToList()
            };
        }
    }
}
