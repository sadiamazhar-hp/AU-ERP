using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class BPTypeSample
    {
        [Key]
        public int Id { get; set; }

        public string TypeName { get; set; } = null!;
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        public ICollection<BusinessPartnerMasterSample> BusinessPartnerMasterSamples { get; set; } = null!;
        public ICollection<BPTypeNumberRanges> BPTypeNumberRanges { get; set; } = null!;
    }
}
