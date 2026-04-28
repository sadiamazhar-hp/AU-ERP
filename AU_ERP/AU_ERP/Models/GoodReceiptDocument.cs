using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

/// <summary>Draft/posted goods receipt document for a production order.</summary>
public class GoodReceiptDocument
{
    [Key]
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }

    [ForeignKey(nameof(ProductionOrderId))]
    public ProductionOrder? ProductionOrder { get; set; }

    [Column(TypeName = "date")]
    public DateTime DocumentDate { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = "";

    [Required]
    [MaxLength(64)]
    public string BatchNo { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal ProducedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtyFirstQuality { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtySecondQuality { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal QtyThirdQuality { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RejectedScrapQty { get; set; }

    public bool IsPosted { get; set; }

    public DateTime? PostedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

