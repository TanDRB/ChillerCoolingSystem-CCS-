using ChillerCoolingSystem_CCS_.Monitoring;

namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Trạng thái hiển thị của một máy đùn tại một thời điểm đọc.</summary>
    public class ExtruderSnapshot
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string Number { get; init; } = string.Empty;

        public RunState RunState { get; init; }

        /// <summary>false khi không kết nối được tới Kepware/PLC — các điểm đo đều NoData.</summary>
        public bool ConnectionOk { get; init; }
        public string? Message { get; init; }

        public DateTime ReadAt { get; init; }
        public IReadOnlyList<PointReading> Points { get; init; } = Array.Empty<PointReading>();

        public int NgCount => Points.Count(p => p.Result == PointResult.Ng);
    }
}

