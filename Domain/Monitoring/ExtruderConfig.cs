namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public class MonitoringOptions
    {
        public const string SectionName = "Monitoring";

        public List<ExtruderConfig> Extruders { get; set; } = new();

        public List<WeightStationConfig> WeightStations { get; set; } = new();
    }

    public class ExtruderConfig
    {
        public string Key { get; set; } = string.Empty;

        public string? PlantKey { get; set; }

        public string Name { get; set; } = "EXTRUDER";

        public string Model { get; set; } = string.Empty;

        public string Number { get; set; } = string.Empty;

        public string? StartStopTag { get; set; }

        public List<TemperaturePointConfig> Points { get; set; } = new();

        public string DisplayName => string.Join(' ', new[] { Name, Number, Model }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public IEnumerable<string> AllTags =>
            Points.Select(p => p.Tag)
                .Append(StartStopTag)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))!;
    }

    public class TemperaturePointConfig
    {
        public string Name { get; set; } = string.Empty;

        public string Tag { get; set; } = string.Empty;

        public double Standard { get; set; }

        public double Tolerance { get; set; }
    }
}
