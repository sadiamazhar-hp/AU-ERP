namespace AU_ERP.Models.ViewModels;

public class SalesOrderListVm
{
    public List<AU_ERP.Models.SalesOrder> Items { get; set; } = new();

    public string? Q { get; set; }

    public string Status { get; set; } = "All";

    public string? PlantId { get; set; }

    public int? DistributionChannelId { get; set; }

    /// <summary>Sales orders that already have a delivery challan (hide “DC” action).</summary>
    public HashSet<int> SalesOrderIdsWithChallan { get; set; } = new();
}
