namespace ChillerCoolingSystem_CCS_.Models.Entities
{
    public class MachineParameter
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public Machine Machine { get; set; } = null!;

        // Hậu tố tag OPC UA (Device + "." + ParameterKey) và key snapshot/SignalR
        // (Machine.Key + ParameterKey, vd "ct3TempOut").
        public string ParameterKey { get; set; } = "";

        // "Temperature" | "Pressure" | "Status" | "Fault" | "ErrorFlag"
        public string Kind { get; set; } = "";

        public string? Unit { get; set; }

        // Key trong window.CCS_I18N (_Layout.cshtml).
        public string LabelKey { get; set; } = "";

        // "temp-red" | "temp-blue" | "temp-gray" | "pressure-teal" | "run-stop" | "fault"
        public string IconKey { get; set; } = "";

        // Chỉ dùng khi Kind = "ErrorFlag": ParameterKey cùng máy mà cờ lỗi này cảnh báo lên.
        public string? LinkedParameterKey { get; set; }

        public int SortOrder { get; set; }
    }
}
