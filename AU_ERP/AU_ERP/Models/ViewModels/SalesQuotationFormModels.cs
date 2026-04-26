namespace AU_ERP.Models.ViewModels;

public class SalesQuotationItemFormRow
{
    public string? MaterialNumber { get; set; }
    /// <summary>Material sales / inventory grade: A, B, C, or Scrap (drives list price per base UOM).</summary>
    public string? SalesPriceGrade { get; set; }
    public int? QuantityUomId { get; set; }
    public decimal? OrderQuantity { get; set; }
    public decimal? NetPrice { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    /// <summary>JSON: chargeId → value for item charge columns (must match header ItemChargeColumnIds).</summary>
    public string? ItemChargeValuesJson { get; set; }
    public string? MaterialDescription { get; set; }
}

public class SalesQuotationCreateFormModel
{
    public string? QuotationNumber { get; set; }
    public int? ConfigurationSchemaId { get; set; }
    public string? PlantId { get; set; }
    public int? DistributionChannelId { get; set; }
    public string? CustomerBusinessPartnerId { get; set; }
    public string? CustomerName { get; set; }
    public string? ShipToAddress { get; set; }
    public string? SalesPersonId { get; set; }
    public string? PriceListCode { get; set; }
    public string? PaymentTerm { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Comma-separated charge Ids: quotation-level value inputs (summary).</summary>
    public string? QuotationLevelChargeIds { get; set; }
    /// <summary>Comma-separated: which schema charges are columns on each line.</summary>
    public string? ItemChargeColumnIds { get; set; }
    /// <summary>JSON: chargeId → value for quotation-level charge inputs.</summary>
    public string? QuotationChargeValuesJson { get; set; }
    public DateTime? QuotationDate { get; set; }
    public DateTime? ValidityDate { get; set; }
    public List<SalesQuotationItemFormRow>? Items { get; set; }
    public string? SubmitAction { get; set; }

    /// <summary>Round-trip list filters when redirecting after save.</summary>
    public string? ReturnQ { get; set; }
    public string? ReturnStatus { get; set; }
    public string? ReturnPlantId { get; set; }
    public int? ReturnDistributionChannelId { get; set; }
}
