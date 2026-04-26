using System;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426120000_AddSalesQuotation")]
    public class AddSalesQuotation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesQuotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DistributionChannelId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShipToAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuotationDate = table.Column<DateTime>(type: "date", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Distribution_Channel_DistributionChannelId",
                        column: x => x.DistributionChannelId,
                        principalTable: "Distribution_Channel",
                        principalColumn: "DistributionChannelID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_SalesQuotationId",
                table: "SalesQuotationItems",
                column: "SalesQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_DistributionChannelId",
                table: "SalesQuotations",
                column: "DistributionChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_PlantId",
                table: "SalesQuotations",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_QuotationNumber",
                table: "SalesQuotations",
                column: "QuotationNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SalesQuotationItems");

            migrationBuilder.DropTable(name: "SalesQuotations");
        }
    }
}
