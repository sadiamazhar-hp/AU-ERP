namespace AU_ERP.Models
{
    /// <summary>One routing work-centre line under an operation (for operation tracking UI).</summary>
    public class OperationStageMachineLineVm
    {
        public int Sequence { get; set; }
        public string DisplayName { get; set; } = "";
        public decimal? LaborTime { get; set; }
        public decimal? MachineTime { get; set; }
        public string? TimeUom { get; set; }
    }
}
