namespace AU_ERP.Models;

public class GoodsIssueIndexVm
{
    public int? FocusProductionOrderId { get; set; }
    public List<GoodsIssueRowVm> Documents { get; set; } = new();
}

public class GoodsIssueRowVm
{
    public GoodsIssueRowVm() { }

    public GoodsIssueRowVm(GoodsIssueDocument d)
    {
        Id = d.Id;
        ProductionOrderId = d.ProductionOrderId;
        DocumentDate = d.DocumentDate;
        DocumentNumber = d.DocumentNumber;
        Status = d.Status;
        CreatedAt = d.CreatedAt;
        CompletedAt = d.CompletedAt;
        ProductionNumber = d.ProductionOrder?.ProductionNumber;
        FinishedMaterialNumber = d.ProductionOrder?.FinishedMaterialNumber;
        FinishedMaterialName = d.ProductionOrder?.FinishedMaterial?.Description;
        DispatchStatus = d.DispatchStatus;
        DispatchSentAt = d.DispatchSentAt;
    }

    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Status { get; set; } = GoodsIssueDocument.StatusDraft;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? ProductionNumber { get; set; }
    public string? FinishedMaterialNumber { get; set; }
    public string? FinishedMaterialName { get; set; }
    public string DispatchStatus { get; set; } = GoodsIssueDocument.DispatchPending;
    public DateTime? DispatchSentAt { get; set; }
}
