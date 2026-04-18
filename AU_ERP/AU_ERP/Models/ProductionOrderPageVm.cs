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
    }
}
