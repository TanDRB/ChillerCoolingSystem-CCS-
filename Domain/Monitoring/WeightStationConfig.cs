namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public class WeightStationConfig
    {
        public string Key { get; set; } = string.Empty;

        public string? PlantKey { get; set; }

        public string Name { get; set; } = "EXTRUDER";
        public string Model { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public string WeightTag { get; set; } = string.Empty;

        // Hệ số từ giá trị thô của tag ra KG (tag tính theo 0,1 kg nên dùng 0.1).
        public double Scale { get; set; } = 1;

        // Giá trị thô nhỏ hơn mức này coi như bàn cân trống (bỏ nhiễu và mức trung gian lúc đặt/nhấc).
        public double MinValue { get; set; } = 0;

        // Sai lệch tối đa (giá trị thô) giữa các lần đọc liên tiếp để coi là đứng yên khi tìm đoạn ổn định.
        public double PlateauTolerance { get; set; } = 3;

        public string? LengthTag { get; set; }

        public string DisplayName => string.Join(' ', new[] { Name, Number, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public IEnumerable<string> AllTags =>
            new[] { WeightTag, LengthTag }.Where(t => !string.IsNullOrWhiteSpace(t))!;
    }
}
