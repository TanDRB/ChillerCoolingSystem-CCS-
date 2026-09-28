namespace ChillerCoolingSystem_CCS_.Models.Dto
{
    public class MachineDto
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Location { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public List<MachineParameterDto> Parameters { get; set; } = new();
    }

    public class MachineParameterDto
    {
        public string ParameterKey { get; set; } = "";
        public string Kind { get; set; } = "";
        public string? Unit { get; set; }
        public string LabelKey { get; set; } = "";
        public string IconKey { get; set; } = "";
        public string? LinkedParameterKey { get; set; }
    }

    public class DashboardZoneDto
    {
        public MachineDto Ct { get; set; } = null!;
        public List<MachineDto> Chillers { get; set; } = new();
    }
}
