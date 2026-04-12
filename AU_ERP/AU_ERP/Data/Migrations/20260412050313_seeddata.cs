using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class seeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add replacement plants first so FK targets exist, then move dependents off legacy IDs "1"–"5".
            migrationBuilder.InsertData(
                table: "PlantsSamples",
                columns: new[] { "PlantID", "PlantName" },
                values: new object[,]
                {
                    { "Emp101", "Emporium" },
                    { "Man102", "Manufacturing Plant" }
                });

            migrationBuilder.Sql(@"
UPDATE [WorkCenterMasterSamples] SET [PlantID] = N'Emp101' WHERE [PlantID] IN (N'1', N'2', N'3', N'4', N'5');
UPDATE [RoutingHeadersSamples] SET [PlantID] = N'Emp101' WHERE [PlantID] IN (N'1', N'2', N'3', N'4', N'5');
UPDATE [ProductionVersions] SET [PlantId] = N'Emp101' WHERE [PlantId] IN (N'1', N'2', N'3', N'4', N'5');
UPDATE [BomHeadersSamples] SET [Plant] = N'Emp101' WHERE [Plant] IN (N'1', N'2', N'3', N'4', N'5');
");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "3");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "4");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PlantsSamples",
                columns: new[] { "PlantID", "PlantName" },
                values: new object[,]
                {
                    { "1", "Pressing" },
                    { "2", "Kiln" },
                    { "3", "Polishing" },
                    { "4", "Cutting" },
                    { "5", "Packing" }
                });

            migrationBuilder.Sql(@"
UPDATE [WorkCenterMasterSamples] SET [PlantID] = N'1' WHERE [PlantID] IN (N'Emp101', N'Man102');
UPDATE [RoutingHeadersSamples] SET [PlantID] = N'1' WHERE [PlantID] IN (N'Emp101', N'Man102');
UPDATE [ProductionVersions] SET [PlantId] = N'1' WHERE [PlantId] IN (N'Emp101', N'Man102');
UPDATE [BomHeadersSamples] SET [Plant] = N'1' WHERE [Plant] IN (N'Emp101', N'Man102');
");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "Emp101");

            migrationBuilder.DeleteData(
                table: "PlantsSamples",
                keyColumn: "PlantID",
                keyValue: "Man102");
        }
    }
}
