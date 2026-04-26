namespace AU_ERP.Models;

public class GlobalUnitConversionFormRow
{
    public int Id { get; set; }
    public int BaseUnitId { get; set; }
    public int AltUnitId { get; set; }
    public decimal Quantity { get; set; }
}
