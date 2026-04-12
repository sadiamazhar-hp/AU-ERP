using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class MaterialType
    {
        [Key]
        public string MaterialTypeCode { get; set; }
        public string Description { get; set; }

        public string? FieldReference { get; set; }

        //Reverse Navigation
        public ICollection<CreateMaterialMaster> CreateMaterialMasters { get; set; }
        public ICollection<MaterialNumberRange> MaterialNumberRanges { get; set; } = null!;
        // public ICollection<BomHeadersSample> BomHeadersSamples { get; set; } = null!;
    }
}
