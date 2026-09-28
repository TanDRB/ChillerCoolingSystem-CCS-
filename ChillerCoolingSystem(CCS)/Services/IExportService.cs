namespace ChillerCoolingSystem_CCS_.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportMachineDataAsync(IEnumerable<string> machineKeys, DateTime fromLocal, DateTime toLocal);
    }
}
