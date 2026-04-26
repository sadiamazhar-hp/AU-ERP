using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260430140000_QuotationChargeValueJson")]
    public class QuotationChargeValueJson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemChargeColumnIds",
                table: "SalesQuotations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotationChargeValuesJson",
                table: "SalesQuotations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemChargeValuesJson",
                table: "SalesQuotationItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemChargeColumnIds",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "QuotationChargeValuesJson",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "ItemChargeValuesJson",
                table: "SalesQuotationItems");
        }
    }
}
