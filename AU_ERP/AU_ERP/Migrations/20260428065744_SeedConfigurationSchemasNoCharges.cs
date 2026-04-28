using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class SeedConfigurationSchemasNoCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [ConfigurationSchemas] WHERE [Title] = N'WalkIn' AND [SchemaType] = 0)
                    INSERT INTO [ConfigurationSchemas] ([Title], [SchemaType]) VALUES (N'WalkIn', 0);

                IF NOT EXISTS (SELECT 1 FROM [ConfigurationSchemas] WHERE [Title] = N'Dealer' AND [SchemaType] = 0)
                    INSERT INTO [ConfigurationSchemas] ([Title], [SchemaType]) VALUES (N'Dealer', 0);

                IF NOT EXISTS (SELECT 1 FROM [ConfigurationSchemas] WHERE [Title] = N'Purchase' AND [SchemaType] = 1)
                    INSERT INTO [ConfigurationSchemas] ([Title], [SchemaType]) VALUES (N'Purchase', 1);
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [ConfigurationSchemas]
                WHERE ([Title] = N'WalkIn' AND [SchemaType] = 0)
                   OR ([Title] = N'Dealer' AND [SchemaType] = 0)
                   OR ([Title] = N'Purchase' AND [SchemaType] = 1);
                """
            );
        }
    }
}
