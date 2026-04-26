using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260429120000_SalesQuotationConfigurationSchema")]
    public class SalesQuotationConfigurationSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfigurationSchemaId",
                table: "SalesQuotations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_ConfigurationSchemaId",
                table: "SalesQuotations",
                column: "ConfigurationSchemaId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_ConfigurationSchemas_ConfigurationSchemaId",
                table: "SalesQuotations",
                column: "ConfigurationSchemaId",
                principalTable: "ConfigurationSchemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_ConfigurationSchemas_ConfigurationSchemaId",
                table: "SalesQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_ConfigurationSchemaId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "ConfigurationSchemaId",
                table: "SalesQuotations");
        }
    }
}
