namespace AU_ERP.Models
{
    public class OperationTrackingPageVm
    {
        public int ProductionOrderId { get; set; }
        public int ProductionNumber { get; set; }
        public string? FinishedItemLabel { get; set; }
        public int TargetQuantity { get; set; }
        public string? UomCode { get; set; }

        /// <summary>Display badge: Completed, In progress, Planned, Released, etc.</summary>
        public string OverallStatusLabel { get; set; } = "";

        public string OrderStatus { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime PlannedStartDate { get; set; }
        public DateTime PlannedEndDate { get; set; }

        /// <summary>Current in-progress stage title, if any.</summary>
        public string? ActiveStageTitle { get; set; }

        public int StagesCompleted { get; set; }
        public int StagesTotal { get; set; }

        public List<ProductionOrderStageProgress> Stages { get; set; } = new();
    }
}
