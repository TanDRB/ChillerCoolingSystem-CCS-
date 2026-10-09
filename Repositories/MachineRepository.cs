using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public class MachineRepository : IMachineRepository
    {
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;

        public MachineRepository(IDbContextFactory<HistoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<Machine>> GetAllWithParametersAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Machines
                .Include(m => m.Parameters.OrderBy(p => p.SortOrder))
                .OrderBy(m => m.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Machine>> GetAllWithParametersAsync(int plantId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Machines
                .Where(m => m.PlantId == plantId)
                .Include(m => m.Parameters.OrderBy(p => p.SortOrder))
                .OrderBy(m => m.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
