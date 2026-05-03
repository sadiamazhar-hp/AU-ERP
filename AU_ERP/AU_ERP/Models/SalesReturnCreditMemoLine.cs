using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesReturnCreditMemoLine
{
    public int Id { get; set; }

    public int SalesReturnCreditMemoId { get; set; }

    [ForeignKey(nameof(SalesReturnCreditMemoId))]
    public SalesReturnCreditMemo? SalesReturnCreditMemo { get; set; }

    public int SalesReturnOrderLineId { get; set; }

    [ForeignKey(nameof(SalesReturnOrderLineId))]
    public SalesReturnOrderLine? SalesReturnOrderLine { get; set; }

    public int LineNo { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReturned { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineCreditAmount { get; set; }
}
