using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class RoutingOperationsSample
    {
        [Key]
        public int OpID { get; set; }
        public int? RoutingID { get; set; }
        public int? WorkCenterID { get; set; }
        public int OperationSequence { get; set; }
        public string? Description { get; set; }
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }
        public string? UoM { get; set; }

        public virtual RoutingHeadersSample? RoutingHeader { get; set; }
        public virtual WorkCenterMasterSample? WorkCenter { get; set; }
    }
}
