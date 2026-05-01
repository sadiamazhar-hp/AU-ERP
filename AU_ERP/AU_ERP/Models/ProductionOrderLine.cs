using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    public class ProductionOrderLine
    {
        [Key]
        public int Id { get; set; }

        public int ProductionOrderId { get; set; }

        public int LineNo { get; set; }

        [Required]
        [MaxLength(450)]
        public string MaterialNumber { get; set; } = null!;

        [MaxLength(500)]
        public string? MaterialDescription { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PlannedQuantity { get; set; }

        public int UomId { get; set; }
        
        [MaxLength(450)]
        public string? PlantId { get; set; }

        [ForeignKey(nameof(ProductionOrderId))]
        public virtual ProductionOrder? ProductionOrder { get; set; }

        [ForeignKey(nameof(MaterialNumber))]
        public virtual CreateMaterialMaster? Material { get; set; }

        [ForeignKey(nameof(UomId))]
        public virtual UnitOfMeasurement? Uom { get; set; }

        [ForeignKey(nameof(PlantId))]
        public virtual PlantsSample? Plant { get; set; }
    }
}
