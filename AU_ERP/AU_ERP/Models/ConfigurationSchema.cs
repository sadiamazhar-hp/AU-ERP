using System.ComponentModel.DataAnnotations;

namespace AU_ERP.Models;

/// <summary>Configuration master: title and linked charges (many-to-many).</summary>
public class ConfigurationSchema
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    public ConfigurationSchemaType SchemaType { get; set; } = ConfigurationSchemaType.Sales;

    public ICollection<ConfigurationSchemaCharge> SchemaCharges { get; set; } = new List<ConfigurationSchemaCharge>();
}
