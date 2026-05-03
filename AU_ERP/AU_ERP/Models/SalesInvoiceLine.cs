using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesInvoiceLine
{
    public int Id { get; set; }

    public int SalesInvoiceId { get; set; }

    [ForeignKey(nameof(SalesInvoiceId))]
    public SalesInvoice? SalesInvoice { get; set; }

    public int LineNo { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Line amount after discount, before line-level charge columns.</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal LineSubtotal { get; set; }

    /// <summary>Line amount after item charge columns (matches SO line pricing at DC qty).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotal { get; set; }

    [MaxLength(2000)]
    public string? ItemChargeValuesJson { get; set; }

    public int? SourceSalesOrderItemId { get; set; }
}
