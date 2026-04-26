using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428120000_AddMaterialStdCostAndQuotationLineUom")]
    public class AddMaterialStdCostAndQuotationLineUom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityUomId",
                table: "SalesQuotationItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_QuantityUomId",
                table: "SalesQuotationItems",
                column: "QuantityUomId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotationItems_UnitOfMeasurements_QuantityUomId",
                table: "SalesQuotationItems",
                column: "QuantityUomId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotationItems_UnitOfMeasurements_QuantityUomId",
                table: "SalesQuotationItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotationItems_QuantityUomId",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "QuantityUomId",
                table: "SalesQuotationItems");
        }
    }
}
