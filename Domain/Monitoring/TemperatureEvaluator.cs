namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public enum PointResult
    {
        /// <summary>Chưa có dữ liệu hợp lệ (mất tín hiệu, tag lỗi, chưa đọc được).</summary>
        NoData = 0,
        Ok = 1,
        Ng = 2
    }

    public enum RunState
    {
        Unknown = 0,
        Running = 1,
        Stopped = 2
    }

    public sealed record PointReading(
        string Name,
        double Standard,
        double Tolerance,
        double? Actual,
        double? Deviation,
        PointResult Result);

    /// <summary>Logic đánh giá thuần (không đụng PLC/DB) — dễ kiểm thử độc lập.</summary>
    public static class TemperatureEvaluator
    {
        /// <summary>
        /// Chênh lệch = Thực tế − Tiêu chuẩn. OK khi |Chênh lệch| ≤ Dung sai (biên tính là OK);
        /// ngược lại NG ("Vượt ngưỡng"). Không có dữ liệu tốt thì NoData.
        /// </summary>
        public static PointReading Evaluate(TemperaturePointConfig point, PlcTagValue? value)
        {
            if (value is null || !value.IsGood || value.Value is null)
            {
                return new PointReading(point.Name, point.Standard, point.Tolerance, null, null, PointResult.NoData);
            }

            var actual = value.Value.Value;
            var deviation = actual - point.Standard;
            var ok = Math.Abs(deviation) <= point.Tolerance + 1e-9;
            return new PointReading(point.Name, point.Standard, point.Tolerance, actual, deviation,
                ok ? PointResult.Ok : PointResult.Ng);
        }

        public static RunState EvaluateRunState(PlcTagValue? value)
        {
            if (value is null || !value.IsGood || value.Value is null)
            {
                return RunState.Unknown;
            }

            return value.Value.Value > 0 ? RunState.Running : RunState.Stopped;
        }
    }
}

