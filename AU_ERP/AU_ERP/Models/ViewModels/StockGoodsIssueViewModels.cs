namespace AU_ERP.Models;

public class StockGoodsIssueIndexVm
{
    public int? FocusProductionOrderId { get; set; }
    public int? FocusSalesOrderId { get; set; }

    /// <summary>Search GI number, linked document label, or detail hint.</summary>
    public string? Q { get; set; }

    /// <summary>Empty = all; <see cref="GoodsIssueDocument.DispatchPending"/> or <see cref="GoodsIssueDocument.DispatchSent"/>.</summary>
    public string? Dispatch { get; set; }

    /// <summary>Empty = all; <see cref="StockGoodsIssueRowVm.SourceReservation"/> or <see cref="StockGoodsIssueRowVm.SourceSalesOrder"/>.</summary>
    public string? Source { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

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
