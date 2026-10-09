namespace ChillerCoolingSystem_CCS_.Monitoring
{
    /// <summary>
    /// Đọc giá trị các tag từ PLC. Interface nằm ở Domain để Services không phụ thuộc cách kết nối cụ
    /// thể (OPC UA tới Kepware, hay bộ giả lập khi chưa có Kepware) — implementation ở Infrastructure.
    /// </summary>
    public interface IPlcDataReader
    {
        /// <summary>
        /// Trả về kết quả cho MỌI tag được hỏi (tag lỗi có IsGood = false). Ném <see cref="PlcConnectionException"/>
        /// khi không kết nối được tới nguồn dữ liệu.
        /// </summary>
        Task<IReadOnlyDictionary<string, PlcTagValue>> ReadAsync(
            IReadOnlyCollection<string> tagIds, CancellationToken cancellationToken = default);
    }
}

