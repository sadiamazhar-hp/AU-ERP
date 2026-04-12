using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class UnitOfMeasurement
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        public string? Code { get; set; }

        public string? Description { get; set; }
    }
}
