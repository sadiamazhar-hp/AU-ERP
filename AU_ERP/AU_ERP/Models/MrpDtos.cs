namespace AU_ERP.Models
{
    public class MrpRunRequestDto
    {
        public string? MaterialNumber { get; set; }
        public decimal Quantity { get; set; }
        public int UomId { get; set; }

        public string? PlantId { get; set; }
        /// <summary>When true (HALB drill-down), FERT-only restriction is skipped.</summary>
        public bool SubMrp { get; set; }
    }

    public class MrpRunResultRowDto
    {
        public string MaterialNumber { get; set; } = "";
        public string? Description { get; set; }
        public string? MaterialTypeCode { get; set; }
        public decimal RequiredQty { get; set; }
        public string? RequiredUomCode { get; set; }
        public int RequiredUomId { get; set; }
        public decimal OnHandQty { get; set; }
        public bool Shortage { get; set; }
        public bool CanRunSubMrp { get; set; }
        public bool ShowPurchaseStub { get; set; }
        public string? InventoryWarning { get; set; }
    }

    public class MrpRunResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool AllSatisfied { get; set; }
        public List<MrpRunResultRowDto> Rows { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>Read-only BOM line for production order BOM modal.</summary>
    public class MrpBomLineDisplayDto
    {
        public string MaterialNumber { get; set; } = "";
        public string? Description { get; set; }
        public string? MaterialTypeCode { get; set; }
        public decimal Quantity { get; set; }
        public string? UomCode { get; set; }
    }
}
