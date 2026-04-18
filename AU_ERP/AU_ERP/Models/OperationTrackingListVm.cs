namespace AU_ERP.Models
{
    public class OperationTrackingListItemVm
    {
        public int ProductionOrderId { get; set; }
        public int ProductionNumber { get; set; }
        public string? FinishedItemLabel { get; set; }
        public int TargetQuantity { get; set; }
        public string? UomCode { get; set; }
        public string Status { get; set; } = "";
        public string OverallStatusLabel { get; set; } = "";
        public int StagesTotal { get; set; }
        public int StagesCompleted { get; set; }
        public string? ActiveStageTitle { get; set; }
    }

    public class OperationTrackingListVm
    {
        public List<OperationTrackingListItemVm> Orders { get; set; } = new();
    }
}
