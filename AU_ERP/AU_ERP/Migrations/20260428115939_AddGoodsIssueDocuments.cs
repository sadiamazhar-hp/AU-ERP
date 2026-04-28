using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsIssueDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsIssueDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssueDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsIssueDocuments_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsIssueDocumentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsIssueDocumentId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RequiredUomId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssueDocumentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsIssueDocumentLines_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsIssueDocumentLines_GoodsIssueDocuments_GoodsIssueDocumentId",
                        column: x => x.GoodsIssueDocumentId,
                        principalTable: "GoodsIssueDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsIssueDocumentLines_UnitOfMeasurements_RequiredUomId",
                        column: x => x.RequiredUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocumentLines_GoodsIssueDocumentId",
                table: "GoodsIssueDocumentLines",
                column: "GoodsIssueDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocumentLines_MaterialNumber",
                table: "GoodsIssueDocumentLines",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocumentLines_RequiredUomId",
                table: "GoodsIssueDocumentLines",
                column: "RequiredUomId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocuments_DocumentNumber",
                table: "GoodsIssueDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocuments_ProductionOrderId",
                table: "GoodsIssueDocuments",
                column: "ProductionOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsIssueDocumentLines");

            migrationBuilder.DropTable(
                name: "GoodsIssueDocuments");
        }
    }
}
