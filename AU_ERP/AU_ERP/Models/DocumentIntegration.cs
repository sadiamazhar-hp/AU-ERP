using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    /// <summary>
    /// Maps a consumer module key (e.g. SaleQuotation, Batch) to a configured
    /// <see cref="DocumentType"/> whose ranges supply the runtime document number.
    /// </summary>
    public class DocumentIntegration
    {
        [Key]
        public int DocumentIntegrationID { get; set; }

        [Required]
        [MaxLength(32)]
        public string ModuleKey { get; set; } = null!;

        [Required]
        public int DocumentTypeID { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual DocumentType? DocumentType { get; set; }
    }
}
