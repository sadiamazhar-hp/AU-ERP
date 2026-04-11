using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class BomItemsSample
    {
        [Key]
        public int ItemID { get; set; }
        public int? BomID { get; set; }
        public string? MaterialNumber { get; set; }
        public decimal? Quantity { get; set; }
        public string? UoM { get; set; }
        public decimal? ScrapPercentage { get; set; }

        public virtual BomHeadersSample? BomHeadersSample { get; set; }
        public virtual CreateMaterialMaster? CreateMaterialMaster { get; set; }
    }
}
