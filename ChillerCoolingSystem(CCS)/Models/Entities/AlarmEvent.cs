namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class AlarmEvent
    {
        public long Id { get; set; }
        public string MachineKey { get; set; } = "";
        public DateTime StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
    }
}
