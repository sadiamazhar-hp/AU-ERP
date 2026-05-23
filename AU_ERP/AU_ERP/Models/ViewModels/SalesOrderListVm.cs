namespace AU_ERP.Models.ViewModels;

public class SalesOrderListVm
{
    public List<AU_ERP.Models.SalesOrder> Items { get; set; } = new();

    public string? Q { get; set; }

    public string Status { get; set; } = "All";

    public string? PlantId { get; set; }

    public int? DistributionChannelId { get; set; }

    /// <summary>Computed workflow status per sales order id.</summary>
    public Dictionary<int, string> WorkflowStatusById { get; set; } = new();

    public int CountOpen { get; set; }
    public int CountPendingStock { get; set; }
    public int CountPendingGoodReceive { get; set; }
    public int CountPendingDc { get; set; }
    public int CountDeliveryInProcess { get; set; }
    public int CountPendingPayment { get; set; }
    public int CountCompleted { get; set; }

    /// <summary>Sales orders that already have a delivery challan (hide “DC” action).</summary>
    public HashSet<int> SalesOrderIdsWithChallan { get; set; } = new();

    /// <summary>Latest sales goods issue document status by sales order id.</summary>
    public Dictionary<int, string> SalesOrderGiStatusById { get; set; } = new();

    /// <summary>Latest sales goods issue document id by sales order id.</summary>
    public Dictionary<int, int> SalesOrderGiDocIdByOrderId { get; set; } = new();

    public Dictionary<int, string> SalesOrderGiDocumentNumberById { get; set; } = new();

    public Dictionary<int, string> SalesOrderGiDispatchStatusById { get; set; } = new();

    /// <summary>Delivery challan fleet action by sales order id (one challan per order).</summary>
    public Dictionary<int, DeliveryChallanFleetActionVm> DeliveryChallanBySalesOrderId { get; set; } = new();
}
