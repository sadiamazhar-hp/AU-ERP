using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260502120000_DeliveryChallanUomAndStockBatch")]
    public partial class DeliveryChallanUomAndStockBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchOrLot",
                table: "StockInventoryLines",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityUomId",
                table: "DeliveryChallanItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_QuantityUomId",
                table: "DeliveryChallanItems",
                column: "QuantityUomId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallanItems_UnitOfMeasurements_QuantityUomId",
                table: "DeliveryChallanItems",
                column: "QuantityUomId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallanItems_UnitOfMeasurements_QuantityUomId",
                table: "DeliveryChallanItems");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallanItems_QuantityUomId",
                table: "DeliveryChallanItems");

            migrationBuilder.DropColumn(
                name: "QuantityUomId",
                table: "DeliveryChallanItems");

            migrationBuilder.DropColumn(
                name: "BatchOrLot",
                table: "StockInventoryLines");
        }
    }
}
