using ChillerCoolingSystem_CCS_.Models.Entities;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public interface IHistoryRepository
    {
        Task<List<TagHistoryEntry>> GetHistoryForMachinesAsync(IEnumerable<string> machineKeys, DateTime fromUtc, DateTime toUtc);
        Task<List<AlarmEvent>> GetAlarmsForMachinesAsync(IEnumerable<string> machineKeys, DateTime fromUtc, DateTime toUtc);
    }
}
