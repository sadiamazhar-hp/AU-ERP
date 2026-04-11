using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BPGrouping
    {
        [Key]
        public int Id { get; set; }

        public string GroupName { get; set; } = null!;

        public ICollection<BusinessPartnerMasterSample> BusinessPartnerMasterSamples { get; set; } = null!;
    }
}
