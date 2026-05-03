using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesPayment
{
    public const string MethodCash = "cash";
    public const string MethodCheque = "cheque";
    public const string MethodOnline = "online";

    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    public int SalesInvoiceId { get; set; }

    [ForeignKey(nameof(SalesInvoiceId))]
    public SalesInvoice? SalesInvoice { get; set; }

    [Required]
    [MaxLength(40)]
    public string InvoiceDocumentNumber { get; set; } = null!;

    [MaxLength(450)]
    public string? DealerBusinessPartnerId { get; set; }

    [MaxLength(500)]
    public string? DealerDisplayName { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(16)]
    public string PaymentMethod { get; set; } = null!;

    [MaxLength(64)]
    public string? ChequeNumber { get; set; }

    public DateTime CreatedAt { get; set; }
}
