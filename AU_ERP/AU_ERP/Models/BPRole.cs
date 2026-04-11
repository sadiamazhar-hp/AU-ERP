using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class BPRole
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(32)]
        public string RoleCode { get; set; } = null!;

        public string RoleName { get; set; } = null!;

        public ICollection<BusinessPartnerMasterSample> BusinessPartnerMasterSamples { get; set; } = null!;
    }
}
