using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

public class SalesGoodsIssueDocumentLine
{
    [Key]
    public int Id { get; set; }

    public int SalesGoodsIssueDocumentId { get; set; }

    [ForeignKey(nameof(SalesGoodsIssueDocumentId))]
    public SalesGoodsIssueDocument? SalesGoodsIssueDocument { get; set; }

    public int? SalesOrderItemId { get; set; }

    [ForeignKey(nameof(SalesOrderItemId))]
    public SalesOrderItem? SalesOrderItem { get; set; }

    [Required]
    [MaxLength(32)]
    public string MaterialNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MaterialDescription { get; set; }

    [MaxLength(32)]
    public string? SalesPriceGrade { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RequiredQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal IssuedQty { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RemainingQty { get; set; }

    public int RequiredUomId { get; set; }

    [ForeignKey(nameof(RequiredUomId))]
    public UnitOfMeasurement? RequiredUom { get; set; }

    [MaxLength(500)]
    public string? BatchSummary { get; set; }
}
