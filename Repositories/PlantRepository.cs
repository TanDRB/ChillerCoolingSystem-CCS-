using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public class PlantRepository : IPlantRepository
    {
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;

        public PlantRepository(IDbContextFactory<HistoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Plant>> GetActiveAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Plants
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
