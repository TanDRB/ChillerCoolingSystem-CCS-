using ChillerCoolingSystem_CCS_.Data;
using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Repositories
{
    public class HistoryRepository : IHistoryRepository
    {
        private readonly IDbContextFactory<HistoryDbContext> _dbFactory;

        public HistoryRepository(IDbContextFactory<HistoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<TagHistoryEntry>> GetHistoryForMachinesAsync(IEnumerable<string> machineKeys, DateTime fromUtc, DateTime toUtc)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var keys = machineKeys.ToList();

            return await db.TagHistoryEntries
                .Where(e => keys.Contains(e.MachineKey) && e.Good && e.TimestampUtc >= fromUtc && e.TimestampUtc <= toUtc)
                .OrderBy(e => e.TimestampUtc)
                .ToListAsync();
        }

        public async Task<List<AlarmEvent>> GetAlarmsForMachinesAsync(IEnumerable<string> machineKeys, DateTime fromUtc, DateTime toUtc)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var keys = machineKeys.ToList();

            // Lấy alarm còn đang mở HOẶC có phần giao với khoảng [fromUtc, toUtc].
            return await db.AlarmEvents
                .Where(a => keys.Contains(a.MachineKey) && a.StartUtc <= toUtc && (a.EndUtc == null || a.EndUtc >= fromUtc))
                .OrderBy(a => a.StartUtc)
                .ToListAsync();
        }
    }
}
