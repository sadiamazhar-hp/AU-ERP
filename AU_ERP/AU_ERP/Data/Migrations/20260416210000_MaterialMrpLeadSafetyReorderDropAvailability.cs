using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416210000_MaterialMrpLeadSafetyReorderDropAvailability")]
    public class MaterialMrpLeadSafetyReorderDropAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilityCheckCode",
                table: "CreateMaterialMaster");

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "CreateMaterialMaster",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SafetyStock",
                table: "CreateMaterialMaster",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReorderPoint",
                table: "CreateMaterialMaster",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "CreateMaterialMaster");

            migrationBuilder.DropColumn(
                name: "SafetyStock",
                table: "CreateMaterialMaster");

            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "CreateMaterialMaster");

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityCheckCode",
                table: "CreateMaterialMaster",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
