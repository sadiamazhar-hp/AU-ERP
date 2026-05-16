namespace AU_ERP.Models
{
    public class ProductionOrderPageVm
    {
        public List<ProductionOrder> Items { get; set; } = new();

        public int CountPlanned { get; set; }
        public int CountReleased { get; set; }
        public int CountInProgress { get; set; }
        public int CountCompleted { get; set; }

        public string? Q { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }

        public int FilteredTotalQty => Items.Sum(i => i.TargetQuantity);

        public Dictionary<int, int> GoodsIssueIdByProductionOrderId { get; set; } = new();

        public Dictionary<int, string> GoodsIssueDocumentNumberByProductionOrderId { get; set; } = new();

        public Dictionary<int, string> GoodsIssueDispatchStatusByProductionOrderId { get; set; } = new();

        /// <remarks>Reservation GI workflow status: Pending / Received / Completed.</remarks>
        public Dictionary<int, string> GoodsIssueStatusByProductionOrderId { get; set; } = new();
    }
}
