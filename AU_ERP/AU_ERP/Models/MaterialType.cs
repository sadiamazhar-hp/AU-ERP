using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;

namespace AU_ERP.Models
{
    public class MaterialType
    {
        public static readonly string[] AllowedTabs = ["BasicData", "Sales", "Purchasing", "MRP", "Accounting"];

        [Key]
        public string MaterialTypeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string? FieldReference { get; set; }
        public string EnabledTabsJson { get; set; } = "[]";

        [NotMapped]
        public List<string> EnabledTabs
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EnabledTabsJson))
                    return new List<string>();

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(EnabledTabsJson) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                var sanitized = (value ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                EnabledTabsJson = JsonSerializer.Serialize(sanitized);
            }
        }

        //Reverse Navigation
        public ICollection<CreateMaterialMaster> CreateMaterialMasters { get; set; } = new List<CreateMaterialMaster>();
        public ICollection<MaterialNumberRange> MaterialNumberRanges { get; set; } = null!;
        // public ICollection<BomHeadersSample> BomHeadersSamples { get; set; } = null!;
    }
}
