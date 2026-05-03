using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesInvoice
{
    public const string StatusOpen = "Open";
    public const string StatusCollected = "Collected";
    /// <summary>Return order saved; quality inspection pending before inventory and final closure.</summary>
    public const string StatusReturnInProcess = "ReturnInProcess";
    public const string StatusReturned = "Returned";

    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [MaxLength(450)]
    public string? DealerBusinessPartnerId { get; set; }

    [ForeignKey(nameof(DealerBusinessPartnerId))]
    public BusinessPartnerMasterSample? DealerBusinessPartner { get; set; }

    [MaxLength(500)]
    public string? DealerDisplayName { get; set; }

    public int DeliveryChallanId { get; set; }

    [ForeignKey(nameof(DeliveryChallanId))]
    public DeliveryChallan? DeliveryChallan { get; set; }

    [Required]
    [MaxLength(40)]
    public string DcNumber { get; set; } = null!;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal GrandTotal { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StatusOpen;

    [MaxLength(128)]
    public string? PaymentTermsSnapshot { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();

    public SalesPayment? SalesPayment { get; set; }

    public SalesReturnOrder? SalesReturnOrder { get; set; }
}
