using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class DocumentRange
    {
        [Key]
        public int RangeID { get; set; }
        public int? DocumentTypeID { get; set; }
        public long? FromNumber { get; set; }
        public long? ToNumber { get; set; }

        /// <summary>Last issued number; null or 0 means next issue will be <see cref="FromNumber"/>.</summary>
        public long? CurrentNumber { get; set; }

        public virtual DocumentType? DocumentType { get; set; }
    }
}
