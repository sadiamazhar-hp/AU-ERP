using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <summary>
    /// Ensures <c>SalesQuotations.PlantId</c> matches <c>PlantsSamples.PlantID</c> (nvarchar(450)) for FK <c>FK_SalesQuotations_PlantsSamples_PlantId</c>.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260427120000_AlignSalesQuotationPlantIdWithPlantsSample")]
    public class AlignSalesQuotationPlantIdWithPlantsSample : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlantId",
                table: "SalesQuotations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
