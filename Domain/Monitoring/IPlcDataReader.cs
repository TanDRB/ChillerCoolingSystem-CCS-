namespace ChillerCoolingSystem_CCS_.Monitoring
{
    public interface IPlcDataReader
    {
        Task<IReadOnlyDictionary<string, PlcTagValue>> ReadAsync(
            IReadOnlyCollection<string> tagIds, CancellationToken cancellationToken = default);
    }
}
