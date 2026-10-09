namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Mục "Monitoring:Plc" trong appsettings.json — chọn nguồn dữ liệu PLC.</summary>
    public class PlcConnectionOptions
    {
        public const string SectionName = "Monitoring:Plc";

        /// <summary>"Simulated" (giả lập, chưa cần Kepware) hoặc "OpcUa" (đọc thật từ Kepware).</summary>
        public string Mode { get; set; } = "Simulated";

        public OpcUaOptions OpcUa { get; set; } = new();

        public bool UseOpcUa => string.Equals(Mode, "OpcUa", StringComparison.OrdinalIgnoreCase);
    }

    public class OpcUaOptions
    {
        /// <summary>Tên ứng dụng hiển thị phía Kepware (danh sách client / chứng chỉ). Đặt theo tên dự án đích.</summary>
        public string ApplicationName { get; set; } = "Extruder Monitoring";

        /// <summary>Địa chỉ OPC UA server của Kepware, mặc định cổng 49320: opc.tcp://&lt;máy Kepware&gt;:49320</summary>
        public string EndpointUrl { get; set; } = "opc.tcp://localhost:49320";

        /// <summary>Chỉ số namespace chứa tag của Kepware (thường là 2). Tag có dạng "Channel.Device.Tag".</summary>
        public int NamespaceIndex { get; set; } = 2;

        /// <summary>true = dùng chế độ bảo mật mạnh nhất server cung cấp; false = không mã hóa (SecurityPolicy None).</summary>
        public bool UseSecurity { get; set; }

        /// <summary>Để trống = đăng nhập ẩn danh (Anonymous).</summary>
        public string? Username { get; set; }
        public string? Password { get; set; }

        /// <summary>Tự tin cậy chứng chỉ của server (chấp nhận trong mạng nội bộ). Chỉ có tác dụng khi UseSecurity = true.</summary>
        public bool AutoAcceptUntrustedCertificates { get; set; } = true;

        public int SessionTimeoutMs { get; set; } = 60_000;
        public int OperationTimeoutMs { get; set; } = 5_000;

        /// <summary>
        /// true (mặc định) = đăng ký (subscription) rồi đọc giá trị mới nhất từ bộ nhớ, giống OPC Quick Client: tag chậm hoặc
        /// lỗi không chặn tag khác. false = đọc đồng bộ từng lần (mỗi lần có thể mất nhiều giây với tag của thiết bị chậm).
        /// </summary>
        public bool UseSubscription { get; set; } = true;

        /// <summary>Chu kỳ lấy mẫu/đẩy dữ liệu của subscription (ms).</summary>
        public int SamplingIntervalMs { get; set; } = 500;

        /// <summary>Với tag vừa được đăng ký, đợi tối đa chừng này (ms) cho giá trị đầu tiên từ Kepware.</summary>
        public int FirstValueWaitMs { get; set; } = 3_000;

        /// <summary>Thời gian tối đa cho một lần kết nối; quá hạn thì báo mất kết nối ngay (trang không bị treo).</summary>
        public int ConnectTimeoutMs { get; set; } = 3_000;

        /// <summary>Sau khi kết nối thất bại, nghỉ chừng này trước khi thử lại (trong lúc nghỉ trả lỗi ngay lập tức).</summary>
        public int RetryCooldownMs { get; set; } = 5_000;
    }
}

