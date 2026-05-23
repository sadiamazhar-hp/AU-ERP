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

        /// <summary>HALB or FERT — BOM header (assembly) material type.</summary>
        [MaxLength(10)]
        public string? HeaderMaterialTypeCode { get; set; }

        /// <summary>Material this BOM is built for (must match <see cref="HeaderMaterialTypeCode"/>).</summary>
        [MaxLength(450)]
        public string? BomMaterialNumber { get; set; }

        public int? BLevel { get; set; }
        public int? AlternativeBOM { get; set; }
        public string? Plant { get; set; }
        [MaxLength(30)]
        public string BomUsage { get; set; } = "Production";
        [MaxLength(30)]
        public string AlternativeNo { get; set; } = "ALT-1";
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public bool IsDefaultBom { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public decimal? BaseQty { get; set; }

        /// <summary>Logical delete — header and references remain for history; excluded from MRP and BOM master list.</summary>
        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public virtual ICollection<BomItemsSample> BomItemsSamples { get; set; }
        public virtual BOMLevelsSample? BOMLevel { get; set; }
        public virtual PlantsSample? PlantSample { get; set; }
        // public virtual MaterialType? MaterialType { get; set; }
        // public virtual CreateMaterialMaster? MaterialMaster { get; set; }
        public virtual BomHeadersSample? AlternativeBom { get; set; }
        public virtual ICollection<BomHeadersSample> DependentBomsWithThisAlternative { get; set; }
    }
}
