using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Data
{
    public class HistoryDbContext : DbContext
    {
        public HistoryDbContext(DbContextOptions<HistoryDbContext> options) : base(options)
        {
        }

        public DbSet<TagHistoryEntry> TagHistoryEntries => Set<TagHistoryEntry>();
        public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();
        public DbSet<Machine> Machines => Set<Machine>();
        public DbSet<MachineParameter> MachineParameters => Set<MachineParameter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TagHistoryEntry>()
                .HasIndex(e => new { e.MachineKey, e.TimestampUtc });

            modelBuilder.Entity<AlarmEvent>()
                .HasIndex(e => new { e.MachineKey, e.StartUtc });

            modelBuilder.Entity<Machine>()
                .HasIndex(m => m.Key)
                .IsUnique();

            modelBuilder.Entity<MachineParameter>()
                .HasOne(p => p.Machine)
                .WithMany(m => m.Parameters)
                .HasForeignKey(p => p.MachineId);
        }
    }
}
