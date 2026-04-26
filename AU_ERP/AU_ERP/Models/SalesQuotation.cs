using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesQuotation
{
    public const string StatusDraft = "Draft";
    public const string StatusSent = "Sent";

    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string QuotationNumber { get; set; } = null!;

    /// <summary>References <see cref="PlantsSample.PlantID"/> (same width as that column, nvarchar(450)).</summary>
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

    /// <summary>Business partner (sold-to) id from <see cref="BusinessPartnerMasterSample.BPID"/>.</summary>
    [MaxLength(450)]
    public string? CustomerBusinessPartnerId { get; set; }

    [ForeignKey(nameof(CustomerBusinessPartnerId))]
    public BusinessPartnerMasterSample? CustomerBusinessPartner { get; set; }

    /// <summary>Assigned sales person (application user id).</summary>
    [MaxLength(450)]
    public string? SalesPersonId { get; set; }

    [MaxLength(64)]
    public string? PriceListCode { get; set; }

    [MaxLength(64)]
    public string? PaymentTerm { get; set; }

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    /// <summary>Comma-separated <see cref="Charge.Id"/>: which charges show as inputs in the quotation summary.</summary>
    [MaxLength(2000)]
    public string? QuotationLevelChargeIds { get; set; }

    /// <summary>Comma-separated <see cref="Charge.Id"/>: which charges appear as value columns on each line.</summary>
    [MaxLength(2000)]
    public string? ItemChargeColumnIds { get; set; }

    /// <summary>JSON: chargeId → value (user-entered) for quotation-level charge inputs.</summary>
    public string? QuotationChargeValuesJson { get; set; }

    /// <summary>Sold-to party (customer) display name; kept when not linked to BP or for search.</summary>
    [MaxLength(500)]
    public string? CustomerName { get; set; }

    [MaxLength(1000)]
    public string? ShipToAddress { get; set; }

    [Column(TypeName = "date")]
    public DateTime QuotationDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime ValidityDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StatusDraft;

    public ICollection<SalesQuotationItem> Items { get; set; } = new List<SalesQuotationItem>();
}
