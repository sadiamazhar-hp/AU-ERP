using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

        [MaxLength(20)]
        public string? PurchasingGroupCode { get; set; }
        public int? GrProcessingTime { get; set; }

        /// <summary>Unit for GR processing time: HR, DAY, or MIN.</summary>
        [MaxLength(10)]
        [Column("Gr_Processing_UOM")]
        public string? GrProcessingUom { get; set; }
        public string? MrpTypeCode { get; set; }
        public string? ProcurementTypeCode { get; set; }
        public string? StrategyGroup { get; set; }
        /// <summary>Planned lead time in calendar days.</summary>
        public int? LeadTimeDays { get; set; }
        public decimal? SafetyStock { get; set; }
        public int? ReorderPoint { get; set; }
        public string? ValuationClassCode { get; set; }

        // Nav properties: not posted with forms; [ValidateNever] avoids false "required" validation.
        [ValidateNever]
        public MaterialType MaterialType { get; set; } = null!;
        public MaterialGroup? MaterialGroup { get; set; }
        [ValidateNever]
        public ICollection<BomItemsSample> BomItemsSamples { get; set; } = new List<BomItemsSample>();
        [ValidateNever]
        public ICollection<RoutingHeadersSample> RoutingHeadersSamples { get; set; } = new List<RoutingHeadersSample>();

    }
    
}
