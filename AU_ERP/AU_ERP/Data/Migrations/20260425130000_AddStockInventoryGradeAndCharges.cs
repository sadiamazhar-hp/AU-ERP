using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425130000_AddStockInventoryGradeAndCharges")]
    public class AddStockInventoryGradeAndCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "StockInventoryLines",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines",
                columns: new[] { "MaterialNumber", "QuantityUomId", "Status", "Grade" });

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sign = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Charges", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Charges_Symbol",
                table: "Charges",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "StockInventoryLines");
        }
    }
}
