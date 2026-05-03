using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesReturnOrderLine
{
    public int Id { get; set; }

    public int SalesReturnOrderId { get; set; }

    [ForeignKey(nameof(SalesReturnOrderId))]
    public SalesReturnOrder? SalesReturnOrder { get; set; }

    public int SalesInvoiceLineId { get; set; }

    [ForeignKey(nameof(SalesInvoiceLineId))]
    public SalesInvoiceLine? SalesInvoiceLine { get; set; }

    public int LineNo { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityInvoiced { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReturned { get; set; }
}
