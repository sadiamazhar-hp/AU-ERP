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

        /// <summary>Last numeric suffix issued for this range bucket (exclusive floor before first issue: <see cref="FromNumber"/> − 1).</summary>
        public long? LastIssuedNumber { get; set; }

        public virtual DocumentType? DocumentType { get; set; }
    }
}
