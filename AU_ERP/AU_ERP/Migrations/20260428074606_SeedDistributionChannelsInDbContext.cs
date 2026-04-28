using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class SeedDistributionChannelsInDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Distribution_Channel",
                columns: new[] { "DistributionChannelID", "DistributionChannelName" },
                values: new object[,]
                {
                    { 1, "OnCall" },
                    { 2, "Direct Sales" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Distribution_Channel",
                keyColumn: "DistributionChannelID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Distribution_Channel",
                keyColumn: "DistributionChannelID",
                keyValue: 2);
        }
    }
}
