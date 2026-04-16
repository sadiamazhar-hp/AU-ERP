// In-project stand-ins for legacy Razor views that referenced an external AUTiles assembly.
// Keep type and property names aligned with those views so Html.*For helpers compile.

namespace AUTiles.Models
{
    public class BomHeadersSample
    {
    }

    public class RoutingOperationsSample
    {
        public string? OpID { get; set; }
        public string? OperationNo { get; set; }
        public string? WorkCenterID { get; set; }
        public string? Description { get; set; }
        public decimal? MachineTime { get; set; }
        public decimal? LaborTime { get; set; }
        public string? UoM { get; set; }
    }

    public class RoutingOperationHeaderSample
    {
        public ICollection<RoutingOperationsSample>? RoutingOperationsSamples { get; set; }
    }

    public class RoutingHeadersSample
    {
        public string? RoutingID { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantID { get; set; }
        public string? StatusID { get; set; }
        public DateTime? ValidFrom { get; set; }
        public ICollection<RoutingOperationsSample>? RoutingOperationsSamples { get; set; }
        public ICollection<RoutingOperationHeaderSample>? OperationHeaders { get; set; }
    }

    public class BusinessPartnerMasterSample
    {
        public string? BPID { get; set; }
        public string? BPRole { get; set; }
        public string? BPType { get; set; }
        public string? BPGrouping { get; set; }
        public string? FullName { get; set; }
        public string? Street { get; set; }
        public string? HouseNo { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Language { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? ReconAccount { get; set; }
        public string? PaymentTerms { get; set; }
        public string? PaymentMethods { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? DistChannel { get; set; }
        public string? SalesSchema { get; set; }
        public string? PurchSchema { get; set; }
    }

    public class WorkCenterMasterSample
    {
        public int ID { get; set; }
        public string? WorkCenterName { get; set; }
        public string? PlantID { get; set; }
        public string? Description { get; set; }
        public decimal? AvailableCapacity { get; set; }
        public decimal? UtilizationPercentage { get; set; }
        public decimal? SetupTime { get; set; }
        public string? SetupUOM { get; set; }
        public decimal? MachineTime { get; set; }
        public string? MachineUOM { get; set; }
        public decimal? LaborTime { get; set; }
        public string? LaborUOM { get; set; }
    }
}
