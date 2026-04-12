using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class BomHeadersSample
    {
        public BomHeadersSample()
        {
            BomItemsSamples = new HashSet<BomItemsSample>();
            DependentBomsWithThisAlternative = new HashSet<BomHeadersSample>();
        }

        [Key]
        public int BomID { get; set; }

        [MaxLength(20)]
        public string? BOMCode { get; set; }
        public string? BOMTitle { get; set; }

        // public string? MaterialNumber { get; set; }
        // public string? MaterialTypeCode { get; set; }

        public int? BLevel { get; set; }
        public int? AlternativeBOM { get; set; }
        public string? Plant { get; set; }
        public DateTime? ValidFrom { get; set; }
        public decimal? BaseQty { get; set; }

        public virtual ICollection<BomItemsSample> BomItemsSamples { get; set; }
        public virtual BOMLevelsSample? BOMLevel { get; set; }
        public virtual PlantsSample? PlantSample { get; set; }
        // public virtual MaterialType? MaterialType { get; set; }
        // public virtual CreateMaterialMaster? MaterialMaster { get; set; }
        public virtual BomHeadersSample? AlternativeBom { get; set; }
        public virtual ICollection<BomHeadersSample> DependentBomsWithThisAlternative { get; set; }
    }
}
