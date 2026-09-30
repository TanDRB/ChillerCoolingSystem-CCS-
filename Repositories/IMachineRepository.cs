using ChillerCoolingSystem_CCS_.Models.Entities;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public interface IMachineRepository
    {
        Task<List<Machine>> GetAllWithParametersAsync();
    }
}
