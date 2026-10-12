namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public class PlcConnectionOptions
    {
        public const string SectionName = "Monitoring:Plc";

        public string Mode { get; set; } = "Simulated";

        public OpcUaOptions OpcUa { get; set; } = new();

        public bool UseOpcUa => string.Equals(Mode, "OpcUa", StringComparison.OrdinalIgnoreCase);
    }

    public class OpcUaOptions
    {
        public string ApplicationName { get; set; } = "Extruder Monitoring";

        public string EndpointUrl { get; set; } = "opc.tcp://localhost:49320";

        public int NamespaceIndex { get; set; } = 2;

        public bool UseSecurity { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool AutoAcceptUntrustedCertificates { get; set; } = true;

        public int SessionTimeoutMs { get; set; } = 60_000;

        public int OperationTimeoutMs { get; set; } = 5_000;

        public int SamplingIntervalMs { get; set; } = 500;

        public int FirstValueWaitMs { get; set; } = 3_000;

        public int ConnectTimeoutMs { get; set; } = 3_000;

        public int RetryCooldownMs { get; set; } = 5_000;
    }
}
