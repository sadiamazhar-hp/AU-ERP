namespace AU_ERP.Models
{
    public class BPIndexViewModel
    {
        public BusinessPartnerMasterSample Draft { get; set; } = new() { BPID = "" };
        public List<BusinessPartnerMasterSample> Partners { get; set; } = new();
    }
}
