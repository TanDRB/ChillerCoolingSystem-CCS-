namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Cấu hình trạm cân của một máy (vd. Extruder #2 ø250 Out): tag trọng lượng và tag chiều dài.</summary>
    public class WeightStationConfig
    {
        /// <summary>Mã dùng trong đường dẫn và khoá lưu lịch sử cân, ví dụ "2out".</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Key của Plant (bảng Plants) hiển thị trạm này.</summary>
        public string? PlantKey { get; set; }

        public string Name { get; set; } = "EXTRUDER";
        public string Model { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        /// <summary>Tag trọng lượng (KG). Giá trị &gt; 0 ổn định = một lần cân; về 0 = bàn cân trống.</summary>
        public string WeightTag { get; set; } = string.Empty;

        /// <summary>Hệ số nhân từ giá trị thô của tag ra KG (tag tính theo 0,1 kg nên dùng 0.1: 376 -> 37,6 kg).</summary>
        public double Scale { get; set; } = 1;

        /// <summary>
        /// Giá trị thô nhỏ hơn mức này coi như bàn cân trống (bỏ nhiễu 1-2 khi không có hàng và các mức trung gian
        /// lúc đặt/nhấc). Với Scale 0.1, 100 = 10 kg.
        /// </summary>
        public double MinValue { get; set; } = 0;

        /// <summary>
        /// Sai lệch tối đa (theo giá trị thô) giữa các lần đọc liên tiếp để coi là "đứng yên" khi tìm đoạn ổn định
        /// của một lần cân. Cân thật luôn dao động nhẹ nên không dùng so sánh bằng nhau tuyệt đối.
        /// </summary>
        public double PlateauTolerance { get; set; } = 3;

        /// <summary>Tag chiều dài thực tế (MM). Chưa có thì để trống, trang hiện "--".</summary>
        public string? LengthTag { get; set; }

        public string DisplayName => string.Join(' ', new[] { Name, Number, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public IEnumerable<string> AllTags =>
            new[] { WeightTag, LengthTag }.Where(t => !string.IsNullOrWhiteSpace(t))!;
    }
}
