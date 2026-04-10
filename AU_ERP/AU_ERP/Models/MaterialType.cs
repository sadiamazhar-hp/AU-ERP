using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class MaterialType
    {
        [Key]
        public string MaterialTypeCode { get; set; }
        public string Description { get; set; }

        public string FieldReference { get; set; }
    }
}