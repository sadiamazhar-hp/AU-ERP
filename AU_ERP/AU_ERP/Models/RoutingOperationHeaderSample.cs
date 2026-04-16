using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    /// <summary>Logical operation under a routing (title + one or more work centre steps).</summary>
    public class RoutingOperationHeaderSample
    {
        public RoutingOperationHeaderSample()
        {
            RoutingOperationsSamples = new HashSet<RoutingOperationsSample>();
        }

        [Key]
        public int OperationHeaderId { get; set; }

        public int RoutingID { get; set; }

        [MaxLength(500)]
        public string? Title { get; set; }

        /// <summary>Sort order among operation headers for the same routing (e.g. 10, 20, …).</summary>
        public int DisplayOrder { get; set; }

        public virtual RoutingHeadersSample? RoutingHeader { get; set; }
        public virtual ICollection<RoutingOperationsSample> RoutingOperationsSamples { get; set; }
    }
}
