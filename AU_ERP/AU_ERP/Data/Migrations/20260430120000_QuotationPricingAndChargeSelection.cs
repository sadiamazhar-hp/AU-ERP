using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260430120000_QuotationPricingAndChargeSelection")]
    public class QuotationPricingAndChargeSelection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPercent",
                table: "Charges",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerBusinessPartnerId",
                table: "SalesQuotations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerm",
                table: "SalesQuotations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceListCode",
                table: "SalesQuotations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotationLevelChargeIds",
                table: "SalesQuotations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "SalesQuotations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesPersonId",
                table: "SalesQuotations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "SalesQuotationItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ItemAppliedChargeIds",
                table: "SalesQuotationItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LineTaxChargeId",
                table: "SalesQuotationItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialDescription",
                table: "SalesQuotationItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAfterDiscount",
                table: "SalesQuotationItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SalesQuotationItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SalesQuotationItems",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE [SalesQuotationItems]
                SET [SubtotalAfterDiscount] = [NetPrice], [UnitPrice] = 0, [TaxAmount] = 0, [DiscountPercent] = 0
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "SalesQuotationItems",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CustomerBusinessPartnerId",
                table: "SalesQuotations",
                column: "CustomerBusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_LineTaxChargeId",
                table: "SalesQuotationItems",
                column: "LineTaxChargeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotations_BusinessPartnerMasterSamples_CustomerBusinessPartnerId",
                table: "SalesQuotations",
                column: "CustomerBusinessPartnerId",
                principalTable: "BusinessPartnerMasterSamples",
                principalColumn: "BPID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesQuotationItems_Charges_LineTaxChargeId",
                table: "SalesQuotationItems",
                column: "LineTaxChargeId",
                principalTable: "Charges",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotationItems_Charges_LineTaxChargeId",
                table: "SalesQuotationItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_BusinessPartnerMasterSamples_CustomerBusinessPartnerId",
                table: "SalesQuotations");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotationItems_LineTaxChargeId",
                table: "SalesQuotationItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesQuotations_CustomerBusinessPartnerId",
                table: "SalesQuotations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeliveryDate",
                table: "SalesQuotationItems",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "DefaultPercent",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "CustomerBusinessPartnerId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "PaymentTerm",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "PriceListCode",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "QuotationLevelChargeIds",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "SalesPersonId",
                table: "SalesQuotations");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "ItemAppliedChargeIds",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "LineTaxChargeId",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "MaterialDescription",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "SubtotalAfterDiscount",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SalesQuotationItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SalesQuotationItems");
        }
    }
}
