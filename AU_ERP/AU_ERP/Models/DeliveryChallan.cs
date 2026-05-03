using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class DeliveryChallan
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string DeliveryChallanNumber { get; set; } = null!;

    [MaxLength(450)]
    public string? PlantId { get; set; }

    [ForeignKey(nameof(PlantId))]
    public PlantsSample? Plant { get; set; }

    [Required]
    [MaxLength(64)]
    public string DeliveryType { get; set; } = "Standard";

    [MaxLength(450)]
    public string? ShipToBusinessPartnerId { get; set; }

    [ForeignKey(nameof(ShipToBusinessPartnerId))]
    public BusinessPartnerMasterSample? ShipToBusinessPartner { get; set; }

    [MaxLength(500)]
    public string? ShipToDisplayName { get; set; }

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    public int? SalesOrderId { get; set; }

    [ForeignKey(nameof(SalesOrderId))]
    public SalesOrder? SalesOrder { get; set; }

    [MaxLength(40)]
    public string? ReferenceSalesOrderNumber { get; set; }

    public int? DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public Driver? Driver { get; set; }

    public int? VehicleId { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    public ICollection<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
}
