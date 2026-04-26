using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesOrder
{
    public const string StatusOpen = "Open";
    public const string StatusConfirmed = "Confirmed";

    public int Id { get; set; }

    /// <summary>Source quotation, when the order was created from a quotation (SO button).</summary>
    public int? SalesQuotationId { get; set; }

    [ForeignKey(nameof(SalesQuotationId))]
    public SalesQuotation? SalesQuotation { get; set; }

    [Required]
    [MaxLength(40)]
    public string SalesOrderNumber { get; set; } = null!;

    [MaxLength(450)]
    public string? PlantId { get; set; }

    [ForeignKey(nameof(PlantId))]
    public PlantsSample? Plant { get; set; }

    public int? DistributionChannelId { get; set; }

    [ForeignKey(nameof(DistributionChannelId))]
    public DistributionChannel? DistributionChannel { get; set; }

    public int? ConfigurationSchemaId { get; set; }

    [ForeignKey(nameof(ConfigurationSchemaId))]
    public ConfigurationSchema? ConfigurationSchema { get; set; }

    [MaxLength(450)]
    public string? CustomerBusinessPartnerId { get; set; }

    [ForeignKey(nameof(CustomerBusinessPartnerId))]
    public BusinessPartnerMasterSample? CustomerBusinessPartner { get; set; }

    [MaxLength(450)]
    public string? SalesPersonId { get; set; }

    [MaxLength(64)]
    public string? PriceListCode { get; set; }

    [MaxLength(64)]
    public string? PaymentTerm { get; set; }

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    [MaxLength(2000)]
    public string? QuotationLevelChargeIds { get; set; }

    [MaxLength(2000)]
    public string? ItemChargeColumnIds { get; set; }

    public string? QuotationChargeValuesJson { get; set; }

    [MaxLength(500)]
    public string? CustomerName { get; set; }

    [MaxLength(1000)]
    public string? ShipToAddress { get; set; }

    [Column(TypeName = "date")]
    public DateTime OrderDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime RequestedDeliveryDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StatusOpen;

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
