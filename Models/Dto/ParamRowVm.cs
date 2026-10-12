namespace ChillerCoolingSystem_CCS_.Models.Dto
{
    public class ParamRowVm
    {
        public string MachineKey { get; set; } = "";

        public MachineParameterDto Parameter { get; set; } = null!;

        public bool Compact { get; set; }
    }
}
