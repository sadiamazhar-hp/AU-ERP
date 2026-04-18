namespace AU_ERP.Models
{
    public sealed class MaterialUomOptionVm
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool IsBase { get; set; }
        /// <summary>True when this UOM was included only for edit (current row) and is not base nor has a conversion.</summary>
        public bool OutsideAllowedSet { get; set; }
    }
}
