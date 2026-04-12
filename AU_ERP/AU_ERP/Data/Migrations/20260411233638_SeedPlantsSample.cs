using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlantsSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
