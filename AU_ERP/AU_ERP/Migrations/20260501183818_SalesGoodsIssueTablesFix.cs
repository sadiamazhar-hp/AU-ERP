using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class SalesGoodsIssueTablesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesGoodsIssueDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGoodsIssueDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGoodsIssueDocuments_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesGoodsIssueDocumentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesGoodsIssueDocumentId = table.Column<int>(type: "int", nullable: false),
                    SalesOrderItemId = table.Column<int>(type: "int", nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalesPriceGrade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RequiredQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IssuedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RequiredUomId = table.Column<int>(type: "int", nullable: false),
                    BatchSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGoodsIssueDocumentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGoodsIssueDocumentLines_SalesGoodsIssueDocuments_SalesGoodsIssueDocumentId",
                        column: x => x.SalesGoodsIssueDocumentId,
                        principalTable: "SalesGoodsIssueDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesGoodsIssueDocumentLines_SalesOrderItems_SalesOrderItemId",
                        column: x => x.SalesOrderItemId,
                        principalTable: "SalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesGoodsIssueDocumentLines_UnitOfMeasurements_RequiredUomId",
                        column: x => x.RequiredUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoodsIssueDocumentLines_RequiredUomId",
                table: "SalesGoodsIssueDocumentLines",
                column: "RequiredUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoodsIssueDocumentLines_SalesGoodsIssueDocumentId",
                table: "SalesGoodsIssueDocumentLines",
                column: "SalesGoodsIssueDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoodsIssueDocumentLines_SalesOrderItemId",
                table: "SalesGoodsIssueDocumentLines",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoodsIssueDocuments_DocumentNumber",
                table: "SalesGoodsIssueDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoodsIssueDocuments_SalesOrderId",
                table: "SalesGoodsIssueDocuments",
                column: "SalesOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesGoodsIssueDocumentLines");

            migrationBuilder.DropTable(
                name: "SalesGoodsIssueDocuments");
        }
    }
}
