using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

/// <summary>Master conversion: 1 <see cref="BaseUnitId"/> = <see cref="Quantity"/> of <see cref="AltUnitId"/> (e.g. 1 box = 12 piece).</summary>
[Table("GlobalUnitConversions")]
public class GlobalUnitConversion
{
    public int Id { get; set; }

    /// <summary>Globally unique name for this conversion recipe (e.g. conA, conB). Multiple rows may share the same base/alt pair with different titles.</summary>
    [MaxLength(100)]
    public string Title { get; set; } = null!;

    public int BaseUnitId { get; set; }
    public UnitOfMeasurement? BaseUnit { get; set; }

    public int AltUnitId { get; set; }
    public UnitOfMeasurement? AltUnit { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    [Range(0.000001, double.MaxValue)]
    public decimal Quantity { get; set; }
}
