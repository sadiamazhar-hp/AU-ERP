using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class GoodsIssueDocumentLine
{
    [Key]
    public int Id { get; set; }

    public int GoodsIssueDocumentId { get; set; }

    [ForeignKey(nameof(GoodsIssueDocumentId))]
    public GoodsIssueDocument? GoodsIssueDocument { get; set; }

    [Required]
    [MaxLength(450)]
    public string MaterialNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQty { get; set; }

    public int RequiredUomId { get; set; }

    [ForeignKey(nameof(MaterialNumber))]
    public CreateMaterialMaster? Material { get; set; }

    [ForeignKey(nameof(RequiredUomId))]
    public UnitOfMeasurement? RequiredUom { get; set; }
}
