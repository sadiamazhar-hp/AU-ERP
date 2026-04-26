using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425100000_MaterialSalesGradePricesAndQuotationLineGrade")]
    public partial class MaterialSalesGradePricesAndQuotationLineGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalesPriceGradeAPerBaseUom",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPriceGradeBPerBaseUom",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPriceGradeCPerBaseUom",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScrapCostPerBaseUom",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            // Only copy/drop if the legacy column exists (some DBs never had it, or it was never migrated in).
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.CreateMaterialMaster', N'StandardCostPerBaseUom') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[CreateMaterialMaster]
                    SET [SalesPriceGradeAPerBaseUom] = [StandardCostPerBaseUom]
                    WHERE [StandardCostPerBaseUom] IS NOT NULL
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.CreateMaterialMaster', N'StandardCostPerBaseUom') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[CreateMaterialMaster] DROP COLUMN [StandardCostPerBaseUom]
                END
                """);

            migrationBuilder.AddColumn<string>(
                name: "SalesPriceGrade",
                table: "SalesQuotationItems",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesPriceGrade",
                table: "SalesQuotationItems");

            migrationBuilder.AddColumn<decimal>(
                name: "StandardCostPerBaseUom",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [CreateMaterialMaster]
                SET [StandardCostPerBaseUom] = [SalesPriceGradeAPerBaseUom]
                WHERE [SalesPriceGradeAPerBaseUom] IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "SalesPriceGradeAPerBaseUom",
                table: "CreateMaterialMaster");

            migrationBuilder.DropColumn(
                name: "SalesPriceGradeBPerBaseUom",
                table: "CreateMaterialMaster");

            migrationBuilder.DropColumn(
                name: "SalesPriceGradeCPerBaseUom",
                table: "CreateMaterialMaster");

            migrationBuilder.DropColumn(
                name: "ScrapCostPerBaseUom",
                table: "CreateMaterialMaster");
        }
    }
}
