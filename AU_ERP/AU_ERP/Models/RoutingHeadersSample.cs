using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class RoutingHeadersSample
    {
        public RoutingHeadersSample()
        {
            OperationHeaders = new HashSet<RoutingOperationHeaderSample>();
        }

        [Key]
        public int RoutingID { get; set; }
        public string? Title { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantID { get; set; }
        public int? StatusID { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual CreateMaterialMaster? Material { get; set; }
        public virtual PlantsSample? Plant { get; set; }
        public virtual ICollection<RoutingOperationHeaderSample> OperationHeaders { get; set; }
    }
}
