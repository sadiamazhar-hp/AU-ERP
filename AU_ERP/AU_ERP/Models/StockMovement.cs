using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class StockMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string MovementNumber { get; set; } = "";

    [Column(TypeName = "date")]
    public DateTime MovementDate { get; set; }

    [Required]
    [MaxLength(450)]
    public string MaterialNumber { get; set; } = "";

    [Required]
    [MaxLength(32)]
    public string Grade { get; set; } = "";

    [Required]
    [MaxLength(450)]
    public string FromPlantId { get; set; } = "";

    [Required]
    [MaxLength(450)]
    public string ToPlantId { get; set; } = "";

    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityMoved { get; set; }

    /// <summary>Batch/lot numbers moved (comma-separated when multiple batches are consumed).</summary>
    [MaxLength(256)]
    public string? BatchOrLot { get; set; }

    public int QuantityUomId { get; set; }

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(MaterialNumber))]
    public virtual CreateMaterialMaster? Material { get; set; }

    [ForeignKey(nameof(FromPlantId))]
    public virtual PlantsSample? FromPlant { get; set; }

    [ForeignKey(nameof(ToPlantId))]
    public virtual PlantsSample? ToPlant { get; set; }

    [ForeignKey(nameof(QuantityUomId))]
    public virtual UnitOfMeasurement? QuantityUom { get; set; }
}

