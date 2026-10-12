namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class WeighingRecord
    {
        public long Id { get; set; }

        public string StationKey { get; set; } = "";

        public double WeightKg { get; set; }

        public DateTime RecordedAtUtc { get; set; }
    }
}
