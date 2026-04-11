using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BOMLevelsSample
    {
        [Key]
        public int LevelID { get; set; }
        public string LevelName { get; set; } = null!;

        public ICollection<BomHeadersSample> BomHeadersSamples { get; set; } = new HashSet<BomHeadersSample>();
    }
}
