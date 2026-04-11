using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class MaterialNumberRange
    {
        [Key]
        public string RangeID { get; set; }
        public string MaterialTypeCode { get; set; }
        public string FromNumber { get; set; }
        public string ToNumber { get; set; }
        public string CurrentNumber { get; set; } // Nullable
        public bool IsExternal { get; set; } // bit column

        public virtual MaterialType? MaterialType { get; set; }
    }
}
