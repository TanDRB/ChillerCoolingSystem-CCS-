namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class Machine
    {
        public int Id { get; set; }
        public int PlantId { get; set; }
        public Plant Plant { get; set; } = null!;
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Location { get; set; } = "";

        // Null cho Cooling Tower; Chiller trỏ về Key của Cooling Tower phục vụ nó.
        public string? ZoneKey { get; set; }

        public string OpcDevice { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int SortOrder { get; set; }

        public List<MachineParameter> Parameters { get; set; } = new();
    }
}
