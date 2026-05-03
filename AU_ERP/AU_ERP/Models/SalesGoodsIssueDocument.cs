using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesGoodsIssueDocument
{
    public const string StatusPending = "Pending";
    public const string StatusReceived = "Received";

    public const string DispatchPending = "Pending";
    public const string DispatchSent = "Sent";

    [Key]
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    [ForeignKey(nameof(SalesOrderId))]
    public SalesOrder? SalesOrder { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StatusPending;

    [Required]
    [MaxLength(20)]
    public string DispatchStatus { get; set; } = DispatchPending;

    public DateTime? DispatchSentAt { get; set; }

    [MaxLength(450)]
    public string? DispatchSentByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime? ReceivedAt { get; set; }

    [MaxLength(450)]
    public string? ReceivedByUserId { get; set; }

    public ICollection<SalesGoodsIssueDocumentLine> Lines { get; set; } = new List<SalesGoodsIssueDocumentLine>();
}
