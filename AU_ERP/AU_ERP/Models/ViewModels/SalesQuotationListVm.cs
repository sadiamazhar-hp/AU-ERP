namespace AU_ERP.Models.ViewModels;

public class SalesQuotationListVm
{
    public List<AU_ERP.Models.SalesQuotation> Items { get; set; } = new();

    /// <summary>Quotation id → linked sales order status (one order per quotation when present).</summary>
    public Dictionary<int, string> LinkedSalesOrderStatusByQuotationId { get; set; } = new();

    public string? Q { get; set; }

    public string Status { get; set; } = "All";

    public string? PlantId { get; set; }

    public int? DistributionChannelId { get; set; }
}
