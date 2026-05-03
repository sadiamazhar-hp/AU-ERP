using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesReturnOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesReturnOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ReturnReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DealerBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DealerDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DeliveryChallanDocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesOrderRequestedDeliveryDate = table.Column<DateTime>(type: "date", nullable: true),
                    InvoiceDocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InvoiceGrandTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ItemsDeliveredQuantityTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnOrders_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesReturnOrderId = table.Column<int>(type: "int", nullable: false),
                    SalesInvoiceLineId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityInvoiced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnOrderLines_SalesInvoiceLines_SalesInvoiceLineId",
                        column: x => x.SalesInvoiceLineId,
                        principalTable: "SalesInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnOrderLines_SalesReturnOrders_SalesReturnOrderId",
                        column: x => x.SalesReturnOrderId,
                        principalTable: "SalesReturnOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnOrderLines_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnOrderLines_QuantityUomId",
                table: "SalesReturnOrderLines",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnOrderLines_SalesInvoiceLineId",
                table: "SalesReturnOrderLines",
                column: "SalesInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnOrderLines_SalesReturnOrderId_SalesInvoiceLineId",
                table: "SalesReturnOrderLines",
                columns: new[] { "SalesReturnOrderId", "SalesInvoiceLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnOrders_DocumentNumber",
                table: "SalesReturnOrders",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnOrders_SalesInvoiceId",
                table: "SalesReturnOrders",
                column: "SalesInvoiceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesReturnOrderLines");

            migrationBuilder.DropTable(
                name: "SalesReturnOrders");
        }
    }
}
