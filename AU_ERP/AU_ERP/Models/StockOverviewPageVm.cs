namespace AU_ERP.Models
{
    public class StockOverviewPageVm
    {
        public List<StockInventoryLine> Items { get; set; } = new();
        public List<string> AssignedPlantIds { get; set; } = new();

        public bool MissingStorePlantAssignment { get; set; }

        public string? PlantDisplay { get; set; }
        public bool CanAddStockEntry { get; set; } = true;

        public string? Q { get; set; }
        public string? MaterialTypeCode { get; set; }
        public int? UomId { get; set; }
    }
}
