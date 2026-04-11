using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class DocumentType
    {
        [Key]
        public int DocumentTypeID { get; set; }
        public string Description { get; set; } = null!;

        public ICollection<DocumentRange> DocumentRanges { get; set; } = null!;
    }
}
