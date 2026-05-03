using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesReturnQualityInspection
{
    public const string StatusPending = "Pending";
    public const string StatusCompleted = "Completed";

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
    [MaxLength(20)]
    public string Status { get; set; } = StatusPending;

    [MaxLength(450)]
    public string? PlantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ICollection<SalesReturnQualityInspectionLine> Lines { get; set; } = new List<SalesReturnQualityInspectionLine>();
}
