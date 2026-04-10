using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class UnitConversion
    {
        [Key]
        public int Id { get; set; }
        public string MaterialNumber { get; set; }
        public string AltUnitCode { get; set; }
        public float Numerator { get; set; }
        public float Denominator { get; set; }
    }
}
