using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesReturnQualityInspectionLine
{
    public int Id { get; set; }

    public int SalesReturnQualityInspectionId { get; set; }

    [ForeignKey(nameof(SalesReturnQualityInspectionId))]
    public SalesReturnQualityInspection? SalesReturnQualityInspection { get; set; }

    public int SalesReturnOrderLineId { get; set; }

    [ForeignKey(nameof(SalesReturnOrderLineId))]
    public SalesReturnOrderLine? SalesReturnOrderLine { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [MaxLength(64)]
    public string? BatchNumber { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReturned { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(2000)]
    public string? ItemChargeValuesJson { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtyBackToStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtyConvertToRaw { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtyScrap { get; set; }

    [MaxLength(32)]
    public string? ConvertTargetMaterialNumber { get; set; }
}
