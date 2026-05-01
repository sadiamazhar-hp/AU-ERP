using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    /// <summary>One goods receipt / produce batch per production order (GR from production).</summary>
    public class GoodsProduceBatch
    {
        [Key]
        public int Id { get; set; }

        public int ProductionOrderId { get; set; }
        
        public int? ProductionOrderLineId { get; set; }

        [Required]
        [MaxLength(450)]
        public string MaterialNumber { get; set; } = null!;
        
        public int UomId { get; set; }

        [Column(TypeName = "date")]
        public DateTime GrDate { get; set; }

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

        [Required]
        [MaxLength(64)]
        public string BatchNo { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ProductionOrderId))]
        public virtual ProductionOrder? ProductionOrder { get; set; }

        [ForeignKey(nameof(ProductionOrderLineId))]
        public virtual ProductionOrderLine? ProductionOrderLine { get; set; }
        
        [ForeignKey(nameof(MaterialNumber))]
        public virtual CreateMaterialMaster? Material { get; set; }
        
        [ForeignKey(nameof(UomId))]
        public virtual UnitOfMeasurement? Uom { get; set; }
    }
}
