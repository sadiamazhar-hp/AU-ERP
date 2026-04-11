using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BPGrouping
    {
        [Key]
        public string GroupID { get; set; } = null!;
        public string GroupName { get; set; } = null!;

        public ICollection<BusinessPartnerMasterSample> BusinessPartnerMasterSamples { get; set; } = null!;
    }
}
