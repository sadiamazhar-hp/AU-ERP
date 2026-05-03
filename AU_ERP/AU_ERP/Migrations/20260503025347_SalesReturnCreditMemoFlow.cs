using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class SalesReturnCreditMemoFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesReturnCreditMemos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesReturnOrderId = table.Column<int>(type: "int", nullable: false),
                    ReturnOrderDocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SalesInvoiceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DealerBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DealerDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrandTotalCredit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnCreditMemos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnCreditMemos_SalesReturnOrders_SalesReturnOrderId",
                        column: x => x.SalesReturnOrderId,
                        principalTable: "SalesReturnOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnCreditMemoLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesReturnCreditMemoId = table.Column<int>(type: "int", nullable: false),
                    SalesReturnOrderLineId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuantityReturned = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineCreditAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnCreditMemoLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnCreditMemoLines_SalesReturnCreditMemos_SalesReturnCreditMemoId",
                        column: x => x.SalesReturnCreditMemoId,
                        principalTable: "SalesReturnCreditMemos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnCreditMemoLines_SalesReturnOrderLines_SalesReturnOrderLineId",
                        column: x => x.SalesReturnOrderLineId,
                        principalTable: "SalesReturnOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnCreditMemoLines_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnCreditMemoLines_QuantityUomId",
                table: "SalesReturnCreditMemoLines",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnCreditMemoLines_SalesReturnCreditMemoId",
                table: "SalesReturnCreditMemoLines",
                column: "SalesReturnCreditMemoId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnCreditMemoLines_SalesReturnOrderLineId",
                table: "SalesReturnCreditMemoLines",
                column: "SalesReturnOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnCreditMemos_DocumentNumber",
                table: "SalesReturnCreditMemos",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnCreditMemos_SalesReturnOrderId",
                table: "SalesReturnCreditMemos",
                column: "SalesReturnOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesReturnCreditMemoLines");

            migrationBuilder.DropTable(
                name: "SalesReturnCreditMemos");
        }
    }
}
