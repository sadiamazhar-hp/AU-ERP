using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodReceiptDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodReceiptDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProducedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QtyFirstQuality = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QtySecondQuality = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QtyThirdQuality = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RejectedScrapQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodReceiptDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodReceiptDocuments_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodReceiptDocuments_DocumentNumber",
                table: "GoodReceiptDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodReceiptDocuments_ProductionOrderId",
                table: "GoodReceiptDocuments",
                column: "ProductionOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodReceiptDocuments");
        }
    }
}
