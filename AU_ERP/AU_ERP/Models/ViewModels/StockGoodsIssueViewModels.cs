namespace AU_ERP.Models;

public class StockGoodsIssueIndexVm
{
    public int? FocusProductionOrderId { get; set; }
    public int? FocusSalesOrderId { get; set; }
    public List<StockGoodsIssueRowVm> Rows { get; set; } = new();
}

public class StockGoodsIssueRowVm
{
    public const string SourceReservation = "Reservation";
    public const string SourceSalesOrder = "SalesOrder";

    public string SourceKind { get; set; } = SourceReservation;
    public int DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string DispatchStatus { get; set; } = GoodsIssueDocument.DispatchPending;
    public DateTime? DispatchSentAt { get; set; }
    public string DocumentWorkflowStatus { get; set; } = "";
    public int SourceId { get; set; }
    public string SourceLabel { get; set; } = "";
    public string? DetailHint { get; set; }
}
