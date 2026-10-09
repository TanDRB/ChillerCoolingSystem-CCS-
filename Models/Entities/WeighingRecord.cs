namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    /// <summary>Một lần cân đã ghi nhận của một trạm cân (dùng để hiện N2/N3 = 2 lần cân gần nhất).</summary>
    public class WeighingRecord
    {
        public long Id { get; set; }

        /// <summary>Key của <c>WeightStationConfig</c>, ví dụ "2out".</summary>
        public string StationKey { get; set; } = "";

        public double WeightKg { get; set; }

        public DateTime RecordedAtUtc { get; set; }
    }
}
