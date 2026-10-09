namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class Plant
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";

        // Null dùng tiêu đề mặc định ("Chiller & Cooler System Overview") ở _Header.cshtml.
        public string? Title { get; set; }

        // Tên view dưới Views/Home/ dùng để render dashboard của xưởng này.
        public string ViewName { get; set; } = "";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
