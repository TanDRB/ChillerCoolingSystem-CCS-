using ChillerCoolingSystem_CCS_.Monitoring;
using Microsoft.Extensions.Options;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>
    /// Giả lập dữ liệu PLC để chạy/kiểm tra giao diện khi chưa có Kepware: nhiệt độ dao động quanh giá
    /// trị tiêu chuẩn, và điểm đo cuối cùng của mỗi máy định kỳ vượt ngưỡng để thấy trạng thái NG.
    /// </summary>
    public sealed class SimulatedPlcDataReader : IPlcDataReader
    {
        private readonly DateTime _startedAt = DateTime.UtcNow;
        private readonly Dictionary<string, double> _baseValue = new();
        private readonly Dictionary<string, double> _ngValue = new();
        private readonly HashSet<string> _weightTags = new();

        public SimulatedPlcDataReader(IOptions<MonitoringOptions> options)
        {
            foreach (var station in options.Value.WeightStations)
            {
                _weightTags.Add(station.WeightTag);
            }

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

                if (!string.IsNullOrWhiteSpace(extruder.StartStopTag)) _baseValue[extruder.StartStopTag] = 1;
                if (!string.IsNullOrWhiteSpace(extruder.SpeedTag)) _baseValue[extruder.SpeedTag] = 740;
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
                    // Chu kỳ 20 giây: 14 giây có tấm cao su (tăng 0-2 s, ổn định ~370-410 dao động nhẹ, giảm 2 s), 6 giây trống
                    var cycle = (long)(seconds / 20);
                    var t = seconds % 20;
                    var plateau = 370 + (cycle * 17 % 41);
                    double weight;
                    if (t >= 14) weight = 0;
                    else if (t < 2) weight = plateau * t / 2 + 3;
                    else if (t >= 12) weight = plateau * (14 - t) / 2 + 3;
                    else weight = plateau + Math.Round(Math.Sin(t * 7));
                    result[tag] = new PlcTagValue(tag, Math.Round(weight), true, now);
                    continue;
                }

                if (!_baseValue.TryGetValue(tag, out var baseValue))
                {
                    result[tag] = new PlcTagValue(tag, null, false, now, "Tag không tồn tại (giả lập)");
                    continue;
                }

                double value;
                var isStatusOrSpeed = tag.EndsWith("START/STOP", StringComparison.OrdinalIgnoreCase)
                                      || tag.EndsWith("SPEED", StringComparison.OrdinalIgnoreCase);
                if (isStatusOrSpeed)
                {
                    value = baseValue;
                }
                else if (_ngValue.TryGetValue(tag, out var ng) && seconds % 60 >= 40 && seconds % 60 < 52)
                {
                    value = ng; // 12 giây mỗi phút: vượt ngưỡng để thấy NG
                }
                else
                {
                    var phase = (uint)tag.GetHashCode() % 628 / 100.0;
                    value = baseValue + Math.Round(3 * Math.Sin(seconds / 9.0 + phase));
                }

                result[tag] = new PlcTagValue(tag, value, true, now);
            }

            return Task.FromResult<IReadOnlyDictionary<string, PlcTagValue>>(result);
        }
    }
}

