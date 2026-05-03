namespace AU_ERP.Models;

public class SalesGoodsIssueIndexVm
{
    public int? FocusSalesOrderId { get; set; }
    public List<SalesGoodsIssueRowVm> Documents { get; set; } = new();
}

public class SalesGoodsIssueRowVm
{
    public SalesGoodsIssueRowVm() { }

    public SalesGoodsIssueRowVm(SalesGoodsIssueDocument d)
    {
        Id = d.Id;
        SalesOrderId = d.SalesOrderId;
        DocumentDate = d.DocumentDate;
        DocumentNumber = d.DocumentNumber;
        Status = d.Status;
        CreatedAt = d.CreatedAt;
        ReceivedAt = d.ReceivedAt;
        SalesOrderNumber = d.SalesOrder?.SalesOrderNumber;
        CustomerName = d.SalesOrder?.CustomerName;
        DispatchStatus = d.DispatchStatus;
        DispatchSentAt = d.DispatchSentAt;
    }

    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Status { get; set; } = SalesGoodsIssueDocument.StatusPending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string DispatchStatus { get; set; } = SalesGoodsIssueDocument.DispatchPending;
    public DateTime? DispatchSentAt { get; set; }
}
