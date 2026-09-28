using ChillerCoolingSystem_CCS_.Models.Dto;

namespace ChillerCoolingSystem_CCS_.Services
{
    public interface IMachineService
    {
        Task<List<MachineDto>> GetAllAsync();
        Task<List<DashboardZoneDto>> GetDashboardZonesAsync();
    }
}
