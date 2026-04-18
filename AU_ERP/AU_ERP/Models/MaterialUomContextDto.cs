namespace AU_ERP.Models
{
    /// <summary>JSON payload for material-scoped UOM dropdowns (base + unit conversions).</summary>
    public sealed class MaterialUomContextDto
    {
        public string MaterialNumber { get; set; } = "";
        public int? BaseUomId { get; set; }
        public string? BaseUomCode { get; set; }
        public string? SetupError { get; set; }
        public List<MaterialUomOptionVm> Uoms { get; set; } = new();
        public List<int> AllowedUomIds { get; set; } = new();
    }
}
