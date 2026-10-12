namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class MachineParameter
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public Machine Machine { get; set; } = null!;

        public string ParameterKey { get; set; } = "";

        public string Kind { get; set; } = "";

        public string? Unit { get; set; }

        public string LabelKey { get; set; } = "";

        public string IconKey { get; set; } = "";

        public string? LinkedParameterKey { get; set; }

        public int SortOrder { get; set; }
    }
}
