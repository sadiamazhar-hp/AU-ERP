using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DistributionChannelId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationSchemaId = table.Column<int>(type: "int", nullable: true),
                    CustomerBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SalesPersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PriceListCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaymentTerm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationLevelChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeColumnIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShipToAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "date", nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_BusinessPartnerMasterSamples_CustomerBusinessPartnerId",
                        column: x => x.CustomerBusinessPartnerId,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_ConfigurationSchemas_ConfigurationSchemaId",
                        column: x => x.ConfigurationSchemaId,
                        principalTable: "ConfigurationSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Distribution_Channel_DistributionChannelId",
                        column: x => x.DistributionChannelId,
                        principalTable: "Distribution_Channel",
                        principalColumn: "DistributionChannelID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SalesPriceGrade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SubtotalAfterDiscount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTaxChargeId = table.Column<int>(type: "int", nullable: true),
                    ItemAppliedChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Charges_LineTaxChargeId",
                        column: x => x.LineTaxChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_LineTaxChargeId",
                table: "SalesOrderItems",
                column: "LineTaxChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_QuantityUomId",
                table: "SalesOrderItems",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId",
                table: "SalesOrderItems",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ConfigurationSchemaId",
                table: "SalesOrders",
                column: "ConfigurationSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerBusinessPartnerId",
                table: "SalesOrders",
                column: "CustomerBusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_DistributionChannelId",
                table: "SalesOrders",
                column: "DistributionChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PlantId",
                table: "SalesOrders",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesOrderNumber",
                table: "SalesOrders",
                column: "SalesOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesQuotationId",
                table: "SalesOrders",
                column: "SalesQuotationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "SalesOrders");
        }
    }
}
