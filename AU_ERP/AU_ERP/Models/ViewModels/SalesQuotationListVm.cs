namespace AU_ERP.Models.ViewModels;

public class SalesQuotationListVm
{
    public List<AU_ERP.Models.SalesQuotation> Items { get; set; } = new();

    public string? Q { get; set; }

    public string Status { get; set; } = "All";

    public string? PlantId { get; set; }

    public int? DistributionChannelId { get; set; }
}
