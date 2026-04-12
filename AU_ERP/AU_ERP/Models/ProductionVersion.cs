using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class ProductionVersion
    {
        [Key]
        public int Id { get; set; }
        public string? PlantId { get; set; }
        public string? Version { get; set; }
        public DateTime? ValidFrom { get; set; }
        public int? BomId { get; set; }
        public int? RoutingId { get; set; }

        public virtual PlantsSample? Plant { get; set; }
        public virtual BomHeadersSample? BomHeader { get; set; }
        public virtual RoutingHeadersSample? RoutingHeader { get; set; }
    }
}
