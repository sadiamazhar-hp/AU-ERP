using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    /// <summary>Per-stage execution tracking for a production order (one row per routing operation header at release).</summary>
    public class ProductionOrderStageProgress
    {
        public const string StagePending = "Pending";
        public const string StageInProgress = "InProgress";
        public const string StageOnHold = "OnHold";
        public const string StageCompleted = "Completed";

        [Key]
        public int Id { get; set; }

        public int ProductionOrderId { get; set; }

        public int RoutingOperationHeaderId { get; set; }

        public int SequenceOrder { get; set; }

        [MaxLength(500)]
        public string? StageTitle { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal PlannedHours { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ActualHours { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? InputQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? OutputQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? WastageQuantity { get; set; }

        [MaxLength(100)]
        public string? WastageReason { get; set; }

        [MaxLength(200)]
        public string? WorkerOperator { get; set; }

        public string? Observations { get; set; }

        [Required]
        [MaxLength(30)]
        public string StageStatus { get; set; } = StagePending;

        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(ProductionOrderId))]
        public virtual ProductionOrder? ProductionOrder { get; set; }

        [ForeignKey(nameof(RoutingOperationHeaderId))]
        public virtual RoutingOperationHeaderSample? RoutingOperationHeader { get; set; }
    }
}
