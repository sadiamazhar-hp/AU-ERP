using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

/// <summary>Financial credit memo generated automatically when a sales return order is saved.</summary>
public class SalesReturnCreditMemo
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    public int SalesReturnOrderId { get; set; }

    [ForeignKey(nameof(SalesReturnOrderId))]
    public SalesReturnOrder? SalesReturnOrder { get; set; }

    [Required]
    [MaxLength(40)]
    public string ReturnOrderDocumentNumber { get; set; } = null!;

    public int SalesInvoiceId { get; set; }

    [Required]
    [MaxLength(40)]
    public string InvoiceDocumentNumber { get; set; } = null!;

    [MaxLength(450)]
    public string? DealerBusinessPartnerId { get; set; }

    [MaxLength(500)]
    public string? DealerDisplayName { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal GrandTotalCredit { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<SalesReturnCreditMemoLine> Lines { get; set; } = new List<SalesReturnCreditMemoLine>();
}
