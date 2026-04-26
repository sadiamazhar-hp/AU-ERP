using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesOrderItem
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    [ForeignKey(nameof(SalesOrderId))]
    public SalesOrder? SalesOrder { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(32)]
    public string? SalesPriceGrade { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OrderQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountPercent { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SubtotalAfterDiscount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal NetPrice { get; set; }

    public int? LineTaxChargeId { get; set; }

    [ForeignKey(nameof(LineTaxChargeId))]
    public Charge? LineTaxCharge { get; set; }

    [MaxLength(2000)]
    public string? ItemAppliedChargeIds { get; set; }

    public string? ItemChargeValuesJson { get; set; }

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "date")]
    public DateTime? DeliveryDate { get; set; }
}
