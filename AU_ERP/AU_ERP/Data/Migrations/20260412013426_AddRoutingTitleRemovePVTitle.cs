using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutingTitleRemovePVTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ProductionVersions");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "RoutingHeadersSamples",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "RoutingHeadersSamples");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ProductionVersions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
