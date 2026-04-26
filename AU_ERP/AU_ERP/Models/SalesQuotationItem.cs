using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesQuotationItem
{
    public int Id { get; set; }

    public int SalesQuotationId { get; set; }

    [ForeignKey(nameof(SalesQuotationId))]
    public SalesQuotation? SalesQuotation { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    /// <summary>Which material sales grade price drives <see cref="UnitPrice"/> (A, B, C, Scrap).</summary>
    [MaxLength(32)]
    public string? SalesPriceGrade { get; set; }

    /// <summary>Unit for order quantity (base or alternate on material master).</summary>
    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OrderQuantity { get; set; }

    /// <summary>User-entered unit list price (PKR) before line discounts/taxes.</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountPercent { get; set; }

    /// <summary>After discount, before line tax (PKR).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal SubtotalAfterDiscount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    /// <summary>Line total including tax; matches UI “line total”.</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal NetPrice { get; set; }

    public int? LineTaxChargeId { get; set; }

    [ForeignKey(nameof(LineTaxChargeId))]
    public Charge? LineTaxCharge { get; set; }

    [MaxLength(2000)]
    public string? ItemAppliedChargeIds { get; set; }

    /// <summary>JSON: chargeId → value for item-level charge column inputs (must match header <see cref="SalesQuotation.ItemChargeColumnIds"/>).</summary>
    public string? ItemChargeValuesJson { get; set; }

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DeliveryDate { get; set; }
}
