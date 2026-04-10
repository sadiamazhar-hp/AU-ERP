using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models
{
    public class CreateMaterialMaster
    {
        [Key]

        public string MaterialNumber { get; set; }
        public string? IndustrySectorCode { get; set; }
        public string? MaterialTypeCode { get; set; }
        public string Description { get; set; }
        public string? BaseUnitCode { get; set; }
        public string? MaterialGroupCode { get; set; }
        public string? Division { get; set; }
        public string? EAN { get; set; }
        public string? DeliveringPlantCode { get; set; }
        public string? ItemCategoryGroup { get; set; }
        public string? PurchasingGroupCode { get; set; }
        public int? GrProcessingTime { get; set; }
        public string? MrpTypeCode { get; set; }
        public string? ProcurementTypeCode { get; set; }
        public string? StrategyGroup { get; set; }
        public string? AvailabilityCheckCode { get; set; }
        public string? ValuationClassCode { get; set; }

        //Nav property
        public MaterialType MaterialType { get; set; }

    }
    
}
