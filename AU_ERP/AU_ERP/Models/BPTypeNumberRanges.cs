using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BPTypeNumberRanges
    {
        [Key]
        public int RangeID { get; set; }
        public int? BPTypeId { get; set; }
        public string? Prefix { get; set; }
        public int StartNumber { get; set; }
        public int EndNumber { get; set; }
        public int CurrentNumber { get; set; }

        public virtual BPTypeSample? TypeSample { get; set; }
    }
}
