using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public sealed class SimulatedPlcDataReader : IPlcDataReader
    {
        private const double WeightCycleSeconds = 20;
        private const double WeightOnScaleSeconds = 14;
        private const double WeightRampSeconds = 2;

        private readonly DateTime _startedAt = DateTime.UtcNow;
        private readonly Dictionary<string, double> _baseValue = new();
        private readonly Dictionary<string, double> _ngValue = new();
        private readonly HashSet<string> _weightTags;

        public SimulatedPlcDataReader(IOptions<MonitoringOptions> options)
        {
            _weightTags = options.Value.WeightStations.Select(station => station.WeightTag).ToHashSet();
            foreach (var extruder in options.Value.Extruders)
            {
                foreach (var point in extruder.Points)
                {
                    _baseValue[point.Tag] = point.Standard;
                }
                var last = extruder.Points.LastOrDefault();
                if (last is not null)
                {
                    _ngValue[last.Tag] = last.Standard - last.Tolerance - 1;
                }
                if (!string.IsNullOrWhiteSpace(extruder.StartStopTag))
                {
                    _baseValue[extruder.StartStopTag] = 1;
                }
            }
        }

        public Task<IReadOnlyDictionary<string, PlcTagValue>> ReadAsync(
            IReadOnlyCollection<string> tagIds, CancellationToken cancellationToken = default)
        {
            var seconds = (DateTime.UtcNow - _startedAt).TotalSeconds;
            var now = DateTime.Now;
            var result = new Dictionary<string, PlcTagValue>();
            foreach (var tag in tagIds)
            {
                if (_weightTags.Contains(tag))
                {
                    result[tag] = new PlcTagValue(tag, SimulateWeight(seconds), true, now);
                }
                else if (_baseValue.TryGetValue(tag, out var baseValue))
                {
                    result[tag] = new PlcTagValue(tag, SimulateValue(tag, baseValue, seconds), true, now);
                }
                else
                {
                    result[tag] = new PlcTagValue(tag, null, false, now, "Tag không tồn tại (giả lập)");
                }
            }
            return Task.FromResult<IReadOnlyDictionary<string, PlcTagValue>>(result);
        }

        private static double SimulateWeight(double seconds)
        {
            var cycle = (long)(seconds / WeightCycleSeconds);
            var t = seconds % WeightCycleSeconds;
            var plateau = 370 + cycle * 17 % 41;
            double weight;
            if (t >= WeightOnScaleSeconds)
            {
                weight = 0;
            }
            else if (t < WeightRampSeconds)
            {
                weight = plateau * t / WeightRampSeconds + 3;
            }
            else if (t >= WeightOnScaleSeconds - WeightRampSeconds)
            {
                weight = plateau * (WeightOnScaleSeconds - t) / WeightRampSeconds + 3;
            }
            else
            {
                weight = plateau + Math.Round(Math.Sin(t * 7));
            }
            return Math.Round(weight);
        }

        private double SimulateValue(string tag, double baseValue, double seconds)
        {
            if (tag.EndsWith("START/STOP", StringComparison.OrdinalIgnoreCase))
            {
                return baseValue;
            }
            var secondOfMinute = seconds % 60;
            if (_ngValue.TryGetValue(tag, out var ng) && secondOfMinute is >= 40 and < 52)
            {
                return ng;
            }
            var phase = (uint)tag.GetHashCode() % 628 / 100.0;
            return baseValue + Math.Round(3 * Math.Sin(seconds / 9.0 + phase));
        }
    }
}
