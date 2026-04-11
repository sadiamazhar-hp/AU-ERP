using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class PlantsSample
    {
        [Key]
        public string PlantID { get; set; } = null!;
        public string PlantName { get; set; } = null!;

        public ICollection<WorkCenterMasterSample> WorkCenters { get; set; } = null!;
        public ICollection<RoutingHeadersSample> RoutingHeaders { get; set; } = null!;
    }
}
