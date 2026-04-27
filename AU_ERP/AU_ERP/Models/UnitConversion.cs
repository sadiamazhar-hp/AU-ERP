using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    public class UnitConversion
    {
        [Key]
        public int Id { get; set; }

        public string MaterialNumber { get; set; } = null!;

        /// <summary>FK to <see cref="UnitOfMeasurement.Id"/> (alternate UOM for this conversion).</summary>
        public int AltUnitId { get; set; }

        public UnitOfMeasurement? AltUnit { get; set; }

        public float Numerator { get; set; }
        public float Denominator { get; set; }

        /// <summary>Which master <see cref="GlobalUnitConversion"/> was chosen when this row was created (for edit/display).</summary>
        public int? GlobalUnitConversionId { get; set; }

        [ForeignKey(nameof(GlobalUnitConversionId))]
        public GlobalUnitConversion? GlobalUnitConversion { get; set; }
    }
}
