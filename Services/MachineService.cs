using ChillerCoolingSystem_CCS_.Models.Dto;
using ChillerCoolingSystem_CCS_.Models.Entities;
using ChillerCoolingSystem_CCS_.Repositories;

namespace ChillerCoolingSystem_CCS_.Services
{
    public class MachineService : IMachineService
    {
        private readonly IMachineRepository _machineRepository;

        public MachineService(IMachineRepository machineRepository)
        {
            _machineRepository = machineRepository;
        }

        public async Task<List<MachineDto>> GetAllAsync()
        {
            var machines = await _machineRepository.GetAllWithParametersAsync();
            return machines.Select(ToDto).ToList();
        }

        public async Task<List<DashboardZoneDto>> GetDashboardZonesAsync()
        {
            var machines = await _machineRepository.GetAllWithParametersAsync();

            var coolingTowers = machines.Where(m => m.ZoneKey == null).OrderBy(m => m.SortOrder);
            var chillersByZone = machines
                .Where(m => m.ZoneKey != null)
                .GroupBy(m => m.ZoneKey)
                .ToDictionary(g => g.Key!, g => g.OrderBy(m => m.SortOrder).ToList());

            return coolingTowers.Select(ct => new DashboardZoneDto
            {
                Ct = ToDto(ct),
                Chillers = (chillersByZone.TryGetValue(ct.Key, out var chillers) ? chillers : new List<Machine>())
                    .Select(ToDto)
                    .ToList()
            }).ToList();
        }

        private static MachineDto ToDto(Machine m) => new()
        {
            Key = m.Key,
            Name = m.Name,
            Type = m.Type,
            Location = m.Location,
            ImageUrl = m.ImageUrl,
            Parameters = m.Parameters.Select(p => new MachineParameterDto
            {
                ParameterKey = p.ParameterKey,
                Kind = p.Kind,
                Unit = p.Unit,
                LabelKey = p.LabelKey,
                IconKey = p.IconKey,
                LinkedParameterKey = p.LinkedParameterKey
            }).ToList()
        };
    }
}
