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

            SeedMachineCatalog(modelBuilder);
        }

        // Danh mục máy/thông số — nguồn dữ liệu duy nhất cho Dashboard và KepwareSmokeTestWorker.
        private static void SeedMachineCatalog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Machine>().HasData(
                new Machine { Id = 1, Key = "ct3", Name = "CT #3", Type = "Cooling Tower", Location = "Banbury #1,2 , Openmill #1,2,7,8", ZoneKey = null, OpcDevice = "CT-CL.BANBURY.CT3", ImageUrl = "/image/CoolingTower.png", SortOrder = 1 },
                new Machine { Id = 2, Key = "ct7", Name = "CT #7", Type = "Cooling Tower", Location = "Banbury #3 , Openmill #10~12", ZoneKey = null, OpcDevice = "CT-CL.BANBURY.CT7", ImageUrl = "/image/CoolingTower.png", SortOrder = 2 },
                new Machine { Id = 3, Key = "cl3", Name = "Chiller #3", Type = "Chiller", Location = "Banbury #2", ZoneKey = "ct3", OpcDevice = "CT-CL.BANBURY.CL3", ImageUrl = "/image/Chiller.png", SortOrder = 3 },
                new Machine { Id = 4, Key = "cl5", Name = "Chiller #5", Type = "Chiller", Location = "Banbury #1", ZoneKey = "ct3", OpcDevice = "CT-CL.BANBURY.CL5", ImageUrl = "/image/Chiller.png", SortOrder = 4 },
                new Machine { Id = 5, Key = "cl8", Name = "Chiller #8", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL8", ImageUrl = "/image/Chiller.png", SortOrder = 5 },
                new Machine { Id = 6, Key = "cl9", Name = "Chiller #9", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL9", ImageUrl = "/image/Chiller.png", SortOrder = 6 },
                new Machine { Id = 7, Key = "cl10", Name = "Chiller #10", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL10", ImageUrl = "/image/Chiller.png", SortOrder = 7 }
            );

            modelBuilder.Entity<MachineParameter>().HasData(
                new MachineParameter { Id = 1, MachineId = 1, ParameterKey = "TempIn", Kind = "Temperature", Unit = "°C", LabelKey = "tempIn", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { Id = 2, MachineId = 1, ParameterKey = "PreWater", Kind = "Pressure", Unit = "kg/cm²", LabelKey = "preWater", IconKey = "pressure-teal", SortOrder = 2 },
                new MachineParameter { Id = 3, MachineId = 1, ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOut", IconKey = "temp-blue", SortOrder = 3 },
                new MachineParameter { Id = 4, MachineId = 1, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 4 },
                new MachineParameter { Id = 5, MachineId = 1, ParameterKey = "TempAmbi", Kind = "Temperature", Unit = "°C", LabelKey = "tempAmbi", IconKey = "temp-gray", SortOrder = 5 },
                new MachineParameter { Id = 6, MachineId = 1, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 6 },
                new MachineParameter { Id = 7, MachineId = 1, ParameterKey = "TempError1", Kind = "ErrorFlag", LinkedParameterKey = "TempOut", SortOrder = 7 },
                new MachineParameter { Id = 8, MachineId = 1, ParameterKey = "TempError2", Kind = "ErrorFlag", LinkedParameterKey = "PreWater", SortOrder = 8 },

                new MachineParameter { Id = 9, MachineId = 2, ParameterKey = "TempIn", Kind = "Temperature", Unit = "°C", LabelKey = "tempIn", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { Id = 10, MachineId = 2, ParameterKey = "PreWater", Kind = "Pressure", Unit = "kg/cm²", LabelKey = "preWater", IconKey = "pressure-teal", SortOrder = 2 },
                new MachineParameter { Id = 11, MachineId = 2, ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOut", IconKey = "temp-blue", SortOrder = 3 },
                new MachineParameter { Id = 12, MachineId = 2, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 4 },
                new MachineParameter { Id = 13, MachineId = 2, ParameterKey = "TempAmbi", Kind = "Temperature", Unit = "°C", LabelKey = "tempAmbi", IconKey = "temp-gray", SortOrder = 5 },
                new MachineParameter { Id = 14, MachineId = 2, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 6 },
                new MachineParameter { Id = 15, MachineId = 2, ParameterKey = "TempError1", Kind = "ErrorFlag", LinkedParameterKey = "TempOut", SortOrder = 7 },
                new MachineParameter { Id = 16, MachineId = 2, ParameterKey = "TempError2", Kind = "ErrorFlag", LinkedParameterKey = "PreWater", SortOrder = 8 },

                new MachineParameter { Id = 17, MachineId = 3, ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { Id = 18, MachineId = 3, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { Id = 19, MachineId = 3, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 },

                new MachineParameter { Id = 20, MachineId = 4, ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { Id = 21, MachineId = 4, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { Id = 22, MachineId = 4, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 },

                // CL8/CL9 không có cảm biến nhiệt độ trên Kepware.
                new MachineParameter { Id = 23, MachineId = 5, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 1 },
                new MachineParameter { Id = 24, MachineId = 5, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 2 },

                new MachineParameter { Id = 25, MachineId = 6, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 1 },
                new MachineParameter { Id = 26, MachineId = 6, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 2 },

                new MachineParameter { Id = 27, MachineId = 7, ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { Id = 28, MachineId = 7, ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { Id = 29, MachineId = 7, ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 }
            );
        }
    }
}
