using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    /// <summary>Manufacturing production order (finished HALB/FERT item).</summary>
    public class ProductionOrder
    {
        public ProductionOrder()
        {
            StageProgresses = new HashSet<ProductionOrderStageProgress>();
        }

        public const string StatusPlanned = "Planned";
        public const string StatusReleased = "Released";
        public const string StatusInProgress = "InProgress";
        public const string StatusCompleted = "Completed";

        public const string PriorityLow = "Low";
        public const string PriorityMedium = "Medium";
        public const string PriorityHigh = "High";

        [Key]
        public int Id { get; set; }

        /// <summary>User-facing document number; first issued value is 40000.</summary>
        public int ProductionNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        [MaxLength(450)]
        public string FinishedMaterialNumber { get; set; } = null!;

        public int TargetQuantity { get; set; }

        public int UomId { get; set; }

        [Column(TypeName = "date")]
        public DateTime PlannedStartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime PlannedEndDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = PriorityMedium;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = StatusPlanned;

        public string? Remarks { get; set; }

        /// <summary>Routing snapshot used when the order was released (operation stages).</summary>
        public int? ReleasedRoutingId { get; set; }

        /// <summary>JSON array of scaled BOM lines (<see cref="MrpBomLineDisplayDto"/>) frozen at release; when set, PO BOM uses this instead of the live material BOM.</summary>
        public string? ReleasedBomSnapshotJson { get; set; }

        [ForeignKey(nameof(FinishedMaterialNumber))]
        public virtual CreateMaterialMaster? FinishedMaterial { get; set; }

        [ForeignKey(nameof(UomId))]
        public virtual UnitOfMeasurement? Uom { get; set; }

        [ForeignKey(nameof(ReleasedRoutingId))]
        public virtual RoutingHeadersSample? ReleasedRouting { get; set; }

        public virtual ICollection<ProductionOrderStageProgress> StageProgresses { get; set; }
    }
}

