using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BPNumberRanges
    {
        [Key]
        public int RangeID { get; set; }
        public string? BPID { get; set; }
        public string? Prefix { get; set; }
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public int CurrentNumber { get; set; }
        public bool? IsActive { get; set; }

        public virtual BusinessPartnerMasterSample? BusinessPartner { get; set; }
    }
}
