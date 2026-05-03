using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class GoodsIssueDocumentLine
{
    [Key]
    public int Id { get; set; }

    public int GoodsIssueDocumentId { get; set; }
    public int? ProductionOrderLineId { get; set; }

    [ForeignKey(nameof(GoodsIssueDocumentId))]
    public GoodsIssueDocument? GoodsIssueDocument { get; set; }

    [Required]
    [MaxLength(450)]
    public string MaterialNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }
    
    [MaxLength(450)]
    public string? FertMaterialNumber { get; set; }
    
    [MaxLength(500)]
    public string? FertMaterialDescription { get; set; }
    
    public int? SelectedBomId { get; set; }
    
    [MaxLength(30)]
    public string? SelectedBomAlternative { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQty { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal IssuedQty { get; set; }
    
    [Column(TypeName = "decimal(18,4)")]
    public decimal RemainingQty { get; set; }

    public int RequiredUomId { get; set; }

    [ForeignKey(nameof(MaterialNumber))]
    public CreateMaterialMaster? Material { get; set; }
    
    [ForeignKey(nameof(FertMaterialNumber))]
    public CreateMaterialMaster? FertMaterial { get; set; }
    
    [ForeignKey(nameof(ProductionOrderLineId))]
    public ProductionOrderLine? ProductionOrderLine { get; set; }
    
    [ForeignKey(nameof(SelectedBomId))]
    public BomHeadersSample? SelectedBom { get; set; }

    [ForeignKey(nameof(RequiredUomId))]
    public UnitOfMeasurement? RequiredUom { get; set; }
}
