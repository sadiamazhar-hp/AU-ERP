namespace AU_ERP.Models;

public class GoodsReceiptPostDto
{
    public int ProductionOrderId { get; set; }
    public DateTime GrDate { get; set; }
    public decimal ProducedQty { get; set; }
    public decimal QtyFirstQuality { get; set; }
    public decimal QtySecondQuality { get; set; }
    public decimal QtyThirdQuality { get; set; }
    public decimal RejectedScrapQty { get; set; }
    public string? BatchNo { get; set; }
    public List<GoodsReceiptPostLineDto> Lines { get; set; } = new();
}

public class GoodsReceiptPostLineDto
{
    public int? ProductionOrderLineId { get; set; }
    public string? MaterialNumber { get; set; }
    public int UomId { get; set; }
    public decimal ProducedQty { get; set; }
    public decimal QtyFirstQuality { get; set; }
    public decimal QtySecondQuality { get; set; }
    public decimal QtyThirdQuality { get; set; }
    public decimal RejectedScrapQty { get; set; }
}

