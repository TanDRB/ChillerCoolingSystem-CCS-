namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class Plant
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";

        public string? Title { get; set; }

        public string ViewName { get; set; } = "";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
