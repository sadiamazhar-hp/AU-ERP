using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class MaterialGroup
    {
        [Key]
        public string MaterialGroupCode { get; set; } = null!;
        public string? Description { get; set; }
        public string? AuthorizationGroup { get; set; }

        public ICollection<CreateMaterialMaster> CreateMaterialMasters { get; set; } = null!;
    }
}
