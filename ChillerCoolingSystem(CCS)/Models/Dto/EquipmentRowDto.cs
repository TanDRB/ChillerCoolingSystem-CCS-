namespace ChillerCoolingSystem_CCS_.Models.Dto
{
    public class EquipmentRowDto
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Location { get; set; } = "";
        public string? TempIn { get; set; }
        public string? TempOut { get; set; }
        public string RunStop { get; set; } = "--";
        public string Fault { get; set; } = "--";
        public string Status { get; set; } = "Offline";
    }
}
