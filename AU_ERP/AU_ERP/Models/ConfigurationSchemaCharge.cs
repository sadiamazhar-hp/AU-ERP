namespace AU_ERP.Models;

public class ConfigurationSchemaCharge
{
    public int Id { get; set; }

    public int ConfigurationSchemaId { get; set; }
    public ConfigurationSchema? ConfigurationSchema { get; set; }

    public int ChargeId { get; set; }
    public Charge? Charge { get; set; }
}
