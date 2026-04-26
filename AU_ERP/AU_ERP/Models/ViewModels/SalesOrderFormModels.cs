namespace AU_ERP.Models.ViewModels;

public class SalesOrderCreateFormModel
{
    public string? SalesOrderNumber { get; set; }
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
    public string? QuotationLevelChargeIds { get; set; }
    public string? ItemChargeColumnIds { get; set; }
    public string? QuotationChargeValuesJson { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public List<SalesQuotationItemFormRow>? Items { get; set; }
    public string? SubmitAction { get; set; }

    public string? ReturnQ { get; set; }
    public string? ReturnStatus { get; set; }
    public string? ReturnPlantId { get; set; }
    public int? ReturnDistributionChannelId { get; set; }
}
