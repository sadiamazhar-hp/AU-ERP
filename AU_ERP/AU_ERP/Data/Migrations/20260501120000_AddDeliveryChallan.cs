using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260501120000_AddDeliveryChallan")]
    public partial class AddDeliveryChallan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryChallans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryChallanNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeliveryType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ShipToBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ShipToDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    ReferenceSalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_BusinessPartnerMasterSamples_ShipToBusinessPartnerId",
                        column: x => x.ShipToBusinessPartnerId,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryChallanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryChallanId = table.Column<int>(type: "int", nullable: false),
                    ReferenceSalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Batch = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SalesOrderItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryChallanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryChallanItems_DeliveryChallans_DeliveryChallanId",
                        column: x => x.DeliveryChallanId,
                        principalTable: "DeliveryChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryChallanItems_SalesOrderItems_SalesOrderItemId",
                        column: x => x.SalesOrderItemId,
                        principalTable: "SalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_DeliveryChallanId",
                table: "DeliveryChallanItems",
                column: "DeliveryChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_SalesOrderItemId",
                table: "DeliveryChallanItems",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_DeliveryChallanNumber",
                table: "DeliveryChallans",
                column: "DeliveryChallanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_PlantId",
                table: "DeliveryChallans",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_SalesOrderId",
                table: "DeliveryChallans",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_ShipToBusinessPartnerId",
                table: "DeliveryChallans",
                column: "ShipToBusinessPartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryChallanItems");

            migrationBuilder.DropTable(
                name: "DeliveryChallans");
        }
    }
}
