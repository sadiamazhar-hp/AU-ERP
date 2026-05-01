namespace AU_ERP.Models;

public class GoodReceiptIndexVm
{
    public int? FocusProductionOrderId { get; set; }
    public List<GoodReceiptRowVm> Documents { get; set; } = new();
}

public class GoodReceiptRowVm
{
    public GoodReceiptRowVm() { }

    public GoodReceiptRowVm(GoodReceiptDocument d)
    {
        Id = d.Id;
        ProductionOrderId = d.ProductionOrderId;
        DocumentDate = d.DocumentDate;
        DocumentNumber = d.DocumentNumber;
        BatchNo = d.BatchNo;
        ProducedQty = d.ProducedQty;
        QtyFirstQuality = d.QtyFirstQuality;
        QtySecondQuality = d.QtySecondQuality;
        QtyThirdQuality = d.QtyThirdQuality;
        RejectedScrapQty = d.RejectedScrapQty;
        IsPosted = d.IsPosted;
        PostedAt = d.PostedAt;

        ProductionNumber = d.ProductionOrder?.ProductionNumber;
        FinishedMaterialNumber = d.ProductionOrder?.FinishedMaterialNumber;
        FinishedMaterialName = d.ProductionOrder?.FinishedMaterial?.Description;
    }

    public int Id { get; set; }
    public int ProductionOrderId { get; set; }

    public DateTime DocumentDate { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string BatchNo { get; set; } = "";

    public decimal ProducedQty { get; set; }
    public decimal QtyFirstQuality { get; set; }
    public decimal QtySecondQuality { get; set; }
    public decimal QtyThirdQuality { get; set; }
    public decimal RejectedScrapQty { get; set; }

    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }

    public int? ProductionNumber { get; set; }
    public string? FinishedMaterialNumber { get; set; }
    public string? FinishedMaterialName { get; set; }
}

public class GoodReceiptDraftSaveDto
{
    public int Id { get; set; }
    public DateTime DocumentDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? BatchNo { get; set; }

    public decimal ProducedQty { get; set; }
    public decimal QtyFirstQuality { get; set; }
    public decimal QtySecondQuality { get; set; }
    public decimal QtyThirdQuality { get; set; }
    public decimal RejectedScrapQty { get; set; }
    public string? DraftLinesJson { get; set; }
}

