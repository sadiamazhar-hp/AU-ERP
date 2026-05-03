using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class SalesReturnQualityInspectionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesReturnQualityInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesReturnOrderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnQualityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnQualityInspections_SalesReturnOrders_SalesReturnOrderId",
                        column: x => x.SalesReturnOrderId,
                        principalTable: "SalesReturnOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnQualityInspectionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesReturnQualityInspectionId = table.Column<int>(type: "int", nullable: false),
                    SalesReturnOrderLineId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ItemChargeValuesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QtyBackToStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyConvertToRaw = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyScrap = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConvertTargetMaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnQualityInspectionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnQualityInspectionLines_SalesReturnOrderLines_SalesReturnOrderLineId",
                        column: x => x.SalesReturnOrderLineId,
                        principalTable: "SalesReturnOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnQualityInspectionLines_SalesReturnQualityInspections_SalesReturnQualityInspectionId",
                        column: x => x.SalesReturnQualityInspectionId,
                        principalTable: "SalesReturnQualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnQualityInspectionLines_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnQualityInspectionLines_QuantityUomId",
                table: "SalesReturnQualityInspectionLines",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnQualityInspectionLines_SalesReturnOrderLineId",
                table: "SalesReturnQualityInspectionLines",
                column: "SalesReturnOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnQualityInspectionLines_SalesReturnQualityInspectionId",
                table: "SalesReturnQualityInspectionLines",
                column: "SalesReturnQualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnQualityInspections_DocumentNumber",
                table: "SalesReturnQualityInspections",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnQualityInspections_SalesReturnOrderId",
                table: "SalesReturnQualityInspections",
                column: "SalesReturnOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesReturnQualityInspectionLines");

            migrationBuilder.DropTable(
                name: "SalesReturnQualityInspections");
        }
    }
}
