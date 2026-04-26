using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models;

/// <summary>Configurable charge (tax, fee) for use in pricing/schemes — master in Configuration.</summary>
public class Charge
{
    public const string TypeNumeric = "Numeric";
    public const string TypePercentage = "Percentage";
    public const string SignAdd = "Add";
    public const string SignSubtract = "Subtract";

    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string Symbol { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    /// <summary><see cref="TypeNumeric"/> or <see cref="TypePercentage"/>.</summary>
    [Required]
    [MaxLength(20)]
    public string ValueType { get; set; } = TypePercentage;

    /// <summary><see cref="SignAdd"/> or <see cref="SignSubtract"/>.</summary>
    [Required]
    [MaxLength(20)]
    public string Sign { get; set; } = SignSubtract;

    /// <summary>When <see cref="ValueType"/> is <see cref="TypePercentage"/>, the default rate (e.g. 17 for 17% GST).</summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? DefaultPercent { get; set; }
}
