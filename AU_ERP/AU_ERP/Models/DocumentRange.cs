using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class DocumentRange
    {
        [Key]
        public int RangeID { get; set; }
        public int? DocumentTypeID { get; set; }
        public int? FromNumber { get; set; }
        public int? ToNumber { get; set; }
        public int? CurrentNumber { get; set; }

        public virtual DocumentType? DocumentType { get; set; }
    }
}
