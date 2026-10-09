using ChillerCoolingSystem_CCS_.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChillerCoolingSystem_CCS_.Data
{
    // Seed danh mục máy lúc khởi động (thay cho HasData trong migration) — HasData
    // buộc EF so khớp toàn bộ seed data với DB thật mỗi lần Migrate, dễ chậm/kẹt khi
    // app đã chạy real-time liên tục. Seed 1 lần khi bảng Machines còn rỗng.
    public static class MachineCatalogSeeder
    {
        public static async Task SeedAsync(HistoryDbContext db, CancellationToken ct = default)
        {
            if (await db.Machines.AnyAsync(ct))
            {
                return;
            }

            var banbury = await db.Plants.FirstAsync(p => p.Key == "banbury", ct);
            var plantId = banbury.Id;

            var ct3 = new Machine { PlantId = plantId, Key = "ct3", Name = "Cooling Tower #3", Type = "Cooling Tower", Location = "Banbury #1,2 , Openmill #1,2,7,8", ZoneKey = null, OpcDevice = "CT-CL.BANBURY.CT3", ImageUrl = "/image/CoolingTower.png", SortOrder = 1 };
            ct3.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "TempIn", Kind = "Temperature", Unit = "°C", LabelKey = "tempIn", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { ParameterKey = "PreWater", Kind = "Pressure", Unit = "kg/cm²", LabelKey = "preWater", IconKey = "pressure-teal", SortOrder = 2 },
                new MachineParameter { ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOut", IconKey = "temp-blue", SortOrder = 3 },
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 4 },
                new MachineParameter { ParameterKey = "TempAmbi", Kind = "Temperature", Unit = "°C", LabelKey = "tempAmbi", IconKey = "temp-gray", SortOrder = 5 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 6 },
                new MachineParameter { ParameterKey = "TempError1", Kind = "ErrorFlag", LinkedParameterKey = "TempOut", SortOrder = 7 },
                new MachineParameter { ParameterKey = "TempError2", Kind = "ErrorFlag", LinkedParameterKey = "PreWater", SortOrder = 8 },
            });

            var ct7 = new Machine { PlantId = plantId, Key = "ct7", Name = "Cooling Tower #7", Type = "Cooling Tower", Location = "Banbury #3 , Openmill #10~12", ZoneKey = null, OpcDevice = "CT-CL.BANBURY.CT7", ImageUrl = "/image/CoolingTower.png", SortOrder = 2 };
            ct7.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "TempIn", Kind = "Temperature", Unit = "°C", LabelKey = "tempIn", IconKey = "temp-red", SortOrder = 1 },
                new MachineParameter { ParameterKey = "PreWater", Kind = "Pressure", Unit = "kg/cm²", LabelKey = "preWater", IconKey = "pressure-teal", SortOrder = 2 },
                new MachineParameter { ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOut", IconKey = "temp-blue", SortOrder = 3 },
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 4 },
                new MachineParameter { ParameterKey = "TempAmbi", Kind = "Temperature", Unit = "°C", LabelKey = "tempAmbi", IconKey = "temp-gray", SortOrder = 5 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 6 },
                new MachineParameter { ParameterKey = "TempError1", Kind = "ErrorFlag", LinkedParameterKey = "TempOut", SortOrder = 7 },
                new MachineParameter { ParameterKey = "TempError2", Kind = "ErrorFlag", LinkedParameterKey = "PreWater", SortOrder = 8 },
            });

            var cl3 = new Machine { PlantId = plantId, Key = "cl3", Name = "Chiller #3", Type = "Chiller", Location = "Banbury #2", ZoneKey = "ct3", OpcDevice = "CT-CL.BANBURY.CL3", ImageUrl = "/image/Chiller.png", SortOrder = 3 };
            cl3.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-blue", SortOrder = 1 },
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 },
            });

            var cl5 = new Machine { PlantId = plantId, Key = "cl5", Name = "Chiller #15", Type = "Chiller", Location = "Banbury #1", ZoneKey = "ct3", OpcDevice = "CT-CL.BANBURY.CL5", ImageUrl = "/image/Chiller.png", SortOrder = 4 };
            cl5.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-blue", SortOrder = 1 },
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 },
            });

            // CL8/CL9 không có cảm biến nhiệt độ trên Kepware.
            var cl8 = new Machine { PlantId = plantId, Key = "cl8", Name = "Chiller #8", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL8", ImageUrl = "/image/Chiller.png", SortOrder = 5 };
            cl8.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 1 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 2 },
            });

            var cl9 = new Machine { PlantId = plantId, Key = "cl9", Name = "Chiller #9", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL9", ImageUrl = "/image/Chiller.png", SortOrder = 6 };
            cl9.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 1 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 2 },
            });

            var cl10 = new Machine { PlantId = plantId, Key = "cl10", Name = "Chiller #10", Type = "Chiller", Location = "Banbury #3", ZoneKey = "ct7", OpcDevice = "CT-CL.BANBURY.CL10", ImageUrl = "/image/Chiller.png", SortOrder = 7 };
            cl10.Parameters.AddRange(new[]
            {
                new MachineParameter { ParameterKey = "TempOut", Kind = "Temperature", Unit = "°C", LabelKey = "tempOutWater", IconKey = "temp-blue", SortOrder = 1 },
                new MachineParameter { ParameterKey = "RunStop", Kind = "Status", LabelKey = "runStop", IconKey = "run-stop", SortOrder = 2 },
                new MachineParameter { ParameterKey = "Fault", Kind = "Fault", LabelKey = "fault", IconKey = "fault", SortOrder = 3 },
            });

            db.Machines.AddRange(ct3, ct7, cl3, cl5, cl8, cl9, cl10);
            await db.SaveChangesAsync(ct);
        }
    }
}
