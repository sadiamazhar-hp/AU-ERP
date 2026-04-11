using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public partial class BusinessPartnerMasterSample
    {
        [Key]
        public string BPID { get; set; } = null!;
        public int? BPRoleId { get; set; }
        public int? BPTypeId { get; set; }
        public int? BPGroupingId { get; set; }
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
        public DateTime? CreatedAt { get; set; }

        public virtual BPRole? Role { get; set; }
        public virtual BPTypeSample? TypeSample { get; set; }
        public virtual BPGrouping? Grouping { get; set; }
    }
}
