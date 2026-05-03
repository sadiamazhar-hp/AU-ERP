using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models.ViewModels;

public class DeliveryChallanCreateFormModel
{
    [MaxLength(450)]
    public string? PlantId { get; set; }

    [MaxLength(64)]
    public string? DeliveryType { get; set; }

    [MaxLength(450)]
    public string? ShipToBusinessPartnerId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DocumentDate { get; set; }

    public int? SalesOrderId { get; set; }

    [MaxLength(40)]
    public string? ReferenceSalesOrderNumber { get; set; }

    public int? DriverId { get; set; }

    public int? VehicleId { get; set; }

    public List<DeliveryChallanItemFormRow> Items { get; set; } = new();
}

public class DeliveryChallanItemFormRow
{
    [MaxLength(40)]
    public string? ReferenceSalesOrderNumber { get; set; }

    [MaxLength(32)]
    public string? MaterialNumber { get; set; }

    public int? QuantityUomId { get; set; }

    public decimal? DeliveryQuantity { get; set; }

    [MaxLength(64)]
    public string? Batch { get; set; }

    public int? SalesOrderItemId { get; set; }
}
