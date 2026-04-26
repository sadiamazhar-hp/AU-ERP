namespace AU_ERP.Models;

/// <summary>POST row for Schema configuration screen (title + selected charge ids).</summary>
public class ConfigurationSchemaFormRow
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public int[]? SelectedChargeIds { get; set; }

    /// <summary>0 = Sales, 1 = Purchase (matches <see cref="ConfigurationSchemaType"/>).</summary>
    public int SchemaType { get; set; }
}
