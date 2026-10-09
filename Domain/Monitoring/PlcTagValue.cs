namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>Giá trị một tag đọc từ PLC (qua Kepware). Value là số (kiểu Word/Int/Float đều quy về double).</summary>
    public sealed record PlcTagValue(string TagId, double? Value, bool IsGood, DateTime? Timestamp = null, string? Error = null);

    /// <summary>Không kết nối được tới nguồn dữ liệu PLC (Kepware tắt, sai địa chỉ, mất mạng...).</summary>
    public sealed class PlcConnectionException : Exception
    {
        public PlcConnectionException(string message, Exception? inner = null) : base(message, inner)
        {
        }
    }
}

