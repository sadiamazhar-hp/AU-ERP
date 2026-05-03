namespace AU_ERP.Models.ViewModels;

public sealed class ReorderQiIndexVm
{
    public List<ReorderQiIndexRowVm> Items { get; set; } = new();
}

public sealed class ReorderQiIndexRowVm
{
    public int QiId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = "";
    public string InvoiceDocumentNumber { get; set; } = "";
    public string ReturnOrderDocumentNumber { get; set; } = "";
}

public sealed class ReorderQiDetailsVm
{
    public int QiId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = "";
    public string? PlantId { get; set; }
    public string InvoiceDocumentNumber { get; set; } = "";
    public string ReturnOrderDocumentNumber { get; set; } = "";
    public List<ReorderQiDetailsLineVm> Lines { get; set; } = new();
}

public sealed class ReorderQiDetailsLineVm
{
    public int LineId { get; set; }
    public string MaterialNumber { get; set; } = "";
    public string? MaterialDescription { get; set; }
    public string? BatchNumber { get; set; }
    public decimal QuantityReturned { get; set; }
    public string? UomCode { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ItemChargeValuesJson { get; set; }
    public decimal QtyBackToStock { get; set; }
    public decimal QtyConvertToRaw { get; set; }
    public decimal QtyScrap { get; set; }
    /// <summary>True when an active BOM exists with at least one ROH line (convert posts all such components).</summary>
    public bool HasBomRoh { get; set; }
}
