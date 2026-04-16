using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416240000_BomHeaderMaterialTypeAndNumber")]
    public class BomHeaderMaterialTypeAndNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeaderMaterialTypeCode",
                table: "BomHeadersSamples",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BomMaterialNumber",
                table: "BomHeadersSamples",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderMaterialTypeCode",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "BomMaterialNumber",
                table: "BomHeadersSamples");
        }
    }
}
