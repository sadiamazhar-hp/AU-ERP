using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BPTypeNumberRanges
    {
        [Key]
        public int RangeID { get; set; }
        public int? BPTypeId { get; set; }
        public string? Prefix { get; set; }
        public long StartNumber { get; set; }
        public long EndNumber { get; set; }

        /// <summary>Last issued number; 0 means next issue will be <see cref="StartNumber"/>.</summary>
        public long CurrentNumber { get; set; }

        public virtual BPTypeSample? TypeSample { get; set; }
    }
}
