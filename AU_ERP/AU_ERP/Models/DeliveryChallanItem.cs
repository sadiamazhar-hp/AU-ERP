using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class DeliveryChallanItem
{
    public int Id { get; set; }

    public int DeliveryChallanId { get; set; }

    [ForeignKey(nameof(DeliveryChallanId))]
    public DeliveryChallan? DeliveryChallan { get; set; }

    [MaxLength(40)]
    public string? ReferenceSalesOrderNumber { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = null!;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DeliveryQuantity { get; set; }

    public int? QuantityUomId { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public UnitOfMeasurement? QuantityUom { get; set; }

    [MaxLength(64)]
    public string? Batch { get; set; }

    public int? SalesOrderItemId { get; set; }

    [ForeignKey(nameof(SalesOrderItemId))]
    public SalesOrderItem? SalesOrderItem { get; set; }
}
