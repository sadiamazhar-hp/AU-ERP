using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    /// <summary>Manual stock overview line (multiple rows per material allowed).</summary>
    public class StockInventoryLine
    {
        public const string StatusActive = "Active";
        public const string StatusNotActive = "NotActive";

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string MaterialNumber { get; set; } = null!;

        /// <summary>Warehouse/manufacturing plant this bin row belongs to.</summary>
        [Required]
        [MaxLength(450)]
        public string PlantID { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; }

        public int QuantityUomId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = StatusActive;

        /// <summary>Quality grade for finished goods (e.g. A, B, C, Scrap). Empty for ungraded materials.</summary>
        [Required]
        [MaxLength(32)]
        public string Grade { get; set; } = string.Empty;

        /// <summary>Standard cost in PKR per quantity UOM.</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StandardCostPerUom { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockValue { get; set; }

        /// <summary>Batch or lot on this bin row (e.g. production lot); shown on issues such as delivery challan.</summary>
        [MaxLength(64)]
        public string? BatchOrLot { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(MaterialNumber))]
        public virtual CreateMaterialMaster? Material { get; set; }

        [ForeignKey(nameof(QuantityUomId))]
        public virtual UnitOfMeasurement? QuantityUom { get; set; }

        [ForeignKey(nameof(PlantID))]
        public virtual PlantsSample? Plant { get; set; }
    }
}
