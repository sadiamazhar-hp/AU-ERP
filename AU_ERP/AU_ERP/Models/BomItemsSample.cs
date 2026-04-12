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

        /// <summary>FK to <see cref="UnitOfMeasurement.Id"/> for component quantity UOM.</summary>
        public int? UomId { get; set; }

        public decimal? ScrapPercentage { get; set; }

        public virtual BomHeadersSample? BomHeadersSample { get; set; }
        public virtual CreateMaterialMaster? CreateMaterialMaster { get; set; }
        public virtual UnitOfMeasurement? Uom { get; set; }
    }
}
