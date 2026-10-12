namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public class ExtruderSnapshot
    {
        public RunState RunState { get; init; }

        public bool ConnectionOk { get; init; }

        public string? Message { get; init; }

        public DateTime ReadAt { get; init; }

        public IReadOnlyList<PointReading> Points { get; init; } = Array.Empty<PointReading>();
    }
}
