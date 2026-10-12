namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public enum PointResult
    {
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

    public static class TemperatureEvaluator
    {
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
