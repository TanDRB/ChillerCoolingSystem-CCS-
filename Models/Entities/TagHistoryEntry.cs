namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class TagHistoryEntry
    {
        public long Id { get; set; }
        public string MachineKey { get; set; } = "";
        public string TagName { get; set; } = "";
        public string? Value { get; set; }
        public bool Good { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
