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
}

