using System.ComponentModel.DataAnnotations;

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
    }
}
