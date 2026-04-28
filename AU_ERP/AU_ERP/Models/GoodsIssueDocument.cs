using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class GoodsIssueDocument
{
    public const string StatusDraft = "Draft";
    public const string StatusPending = "Pending";
    public const string StatusCompleted = "Completed";

    [Key]
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }

    [ForeignKey(nameof(ProductionOrderId))]
    public ProductionOrder? ProductionOrder { get; set; }

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StatusDraft;

    public DateTime CreatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(450)]
    public string? CompletedByUserId { get; set; }

    public virtual ICollection<GoodsIssueDocumentLine> Lines { get; set; } = new List<GoodsIssueDocumentLine>();
}
