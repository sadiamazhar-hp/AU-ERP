using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class RoutingOperationsSample
    {
        [Key]
        public int OpID { get; set; }

        public int OperationHeaderId { get; set; }
        public int? WorkCenterID { get; set; }
        public int OperationSequence { get; set; }
        public string? Description { get; set; }
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }

        /// <summary>Time UOM copied from the work centre: <see cref="WorkCenterTimeUom"/> (Min, Hr, Day).</summary>
        [MaxLength(10)]
        public string? TimeUom { get; set; }

        public virtual RoutingOperationHeaderSample? OperationHeader { get; set; }
        public virtual WorkCenterMasterSample? WorkCenter { get; set; }
    }
}
