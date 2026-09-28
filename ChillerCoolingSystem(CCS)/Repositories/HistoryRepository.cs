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
            var keys = new HashSet<string>(machineKeys);

            // keys.Contains(...) trong WHERE bị EF Core 8 dịch sang OPENJSON(...)
            // WITH (...), lỗi "Incorrect syntax near 'WITH'" trên SQL Server này
            // — lọc machine ở bộ nhớ sau khi đã giới hạn theo thời gian.
            var entries = await db.TagHistoryEntries
                .Where(e => e.Good && e.TimestampUtc >= fromUtc && e.TimestampUtc <= toUtc)
                .OrderBy(e => e.TimestampUtc)
                .ToListAsync();

            return entries.Where(e => keys.Contains(e.MachineKey)).ToList();
        }

        public async Task<List<AlarmEvent>> GetAlarmsForMachinesAsync(IEnumerable<string> machineKeys, DateTime fromUtc, DateTime toUtc)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var keys = new HashSet<string>(machineKeys);

            // Lấy alarm còn đang mở HOẶC có phần giao với khoảng [fromUtc, toUtc].
            var alarms = await db.AlarmEvents
                .Where(a => a.StartUtc <= toUtc && (a.EndUtc == null || a.EndUtc >= fromUtc))
                .OrderBy(a => a.StartUtc)
                .ToListAsync();

            return alarms.Where(a => keys.Contains(a.MachineKey)).ToList();
        }
    }
}
