using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesReturnOrder
{
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
    [MaxLength(2000)]
    public string ReturnReason { get; set; } = null!;

    [MaxLength(450)]
    public string? DealerBusinessPartnerId { get; set; }

    [MaxLength(500)]
    public string? DealerDisplayName { get; set; }

    [MaxLength(40)]
    public string? SalesOrderNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime DeliveryChallanDocumentDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? SalesOrderRequestedDeliveryDate { get; set; }

    [Required]
    [MaxLength(40)]
    public string InvoiceDocumentNumber { get; set; } = null!;

    [Column(TypeName = "decimal(18,4)")]
    public decimal InvoiceGrandTotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ItemsDeliveredQuantityTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<SalesReturnOrderLine> Lines { get; set; } = new List<SalesReturnOrderLine>();

    public SalesReturnQualityInspection? SalesReturnQualityInspection { get; set; }

    public SalesReturnCreditMemo? SalesReturnCreditMemo { get; set; }
}
