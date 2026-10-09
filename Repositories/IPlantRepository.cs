using ChillerCoolingSystem_CCS_.Models.Entities;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public interface IPlantRepository
    {
        Task<List<Plant>> GetActiveAsync();
    }
}
