namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Gốc cấu hình mục "Monitoring" trong appsettings.json.</summary>
    public class MonitoringOptions
    {
        public const string SectionName = "Monitoring";

        public List<ExtruderConfig> Extruders { get; set; } = new();

        public List<WeightStationConfig> WeightStations { get; set; } = new();
    }

    /// <summary>Cấu hình một máy đùn (Extruder): các điểm đo nhiệt độ và tag trạng thái.</summary>
    public class ExtruderConfig
    {
        /// <summary>Mã dùng trong đường dẫn, ví dụ "1" -> /Extruder/1.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Key của Plant (bảng Plants) hiển thị máy này; trống = máy chỉ mở qua ?id=.</summary>
        public string? PlantKey { get; set; }

        public string Name { get; set; } = "EXTRUDER";
        public string Model { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        /// <summary>Tag START/STOP của máy (giá trị &gt; 0 = đang chạy).</summary>
        public string? StartStopTag { get; set; }

        /// <summary>Tag tốc độ (hiện chưa hiển thị trên bảng, dành cho mở rộng).</summary>
        public string? SpeedTag { get; set; }

        public List<TemperaturePointConfig> Points { get; set; } = new();

        public string DisplayName => string.Join(' ', new[] { Name, Number, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public IEnumerable<string> AllTags =>
            Points.Select(p => p.Tag)
                .Concat(new[] { StartStopTag, SpeedTag })
                .Where(t => !string.IsNullOrWhiteSpace(t))!;
    }

    /// <summary>Một điểm đo nhiệt độ: giá trị tiêu chuẩn ± dung sai.</summary>
    public class TemperaturePointConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public double Standard { get; set; }
        public double Tolerance { get; set; }
    }
}

