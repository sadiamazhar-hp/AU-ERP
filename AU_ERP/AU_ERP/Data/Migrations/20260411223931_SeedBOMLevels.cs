using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedBOMLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BOMLevelsSamples",
                columns: new[] { "LevelID", "LevelName" },
                values: new object[,]
                {
                    { 1, "FG" },
                    { 2, "SFG" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BOMLevelsSamples",
                keyColumn: "LevelID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BOMLevelsSamples",
                keyColumn: "LevelID",
                keyValue: 2);
        }
    }
}
