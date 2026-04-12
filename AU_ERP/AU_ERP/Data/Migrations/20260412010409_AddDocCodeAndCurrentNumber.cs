using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocCodeAndCurrentNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocCode",
                table: "DocumentTypes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentNumber",
                table: "DocumentRanges",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocCode",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "CurrentNumber",
                table: "DocumentRanges");
        }
    }
}
