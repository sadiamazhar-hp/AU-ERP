using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class WorkCenterMasterSample
    {
        [Key]
        public int ID { get; set; }
        public string WorkCenterName { get; set; } = null!;
        public string? PlantID { get; set; }
        public string? Description { get; set; }
        public int? AvailableCapacity { get; set; }
        public int? UtilizationPercentage { get; set; }
        public decimal? SetupTime { get; set; }
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }

        /// <summary>Time UOM for setup, machine, and labor: <see cref="WorkCenterTimeUom"/> (Min, Hr, Day).</summary>
        [MaxLength(10)]
        public string? TimeUom { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual PlantsSample? Plant { get; set; }
        public ICollection<RoutingOperationsSample> RoutingOperations { get; set; } = null!;
    }
}
