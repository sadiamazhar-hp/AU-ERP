namespace AU_ERP.Models
{
    public class StockOverviewPageVm
    {
        public List<StockInventoryLine> Items { get; set; } = new();

        public bool MissingStorePlantAssignment { get; set; }

        public string? PlantDisplay { get; set; }

        public string? Q { get; set; }
        public string? MaterialTypeCode { get; set; }
        public int? UomId { get; set; }
    }
}
