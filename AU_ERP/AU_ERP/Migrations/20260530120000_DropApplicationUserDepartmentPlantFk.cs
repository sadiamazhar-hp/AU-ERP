using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <summary>
    /// PlantID on ApplicationUserDepartments stores comma-separated plant codes — remove invalid single-value FK.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260530120000_DropApplicationUserDepartmentPlantFk")]
    public partial class DropApplicationUserDepartmentPlantFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserDepartments_PlantsSamples_PlantID",
                table: "ApplicationUserDepartments");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserDepartments_PlantID",
                table: "ApplicationUserDepartments");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserDepartments_PlantID",
                table: "ApplicationUserDepartments",
                column: "PlantID");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserDepartments_PlantsSamples_PlantID",
                table: "ApplicationUserDepartments",
                column: "PlantID",
                principalTable: "PlantsSamples",
                principalColumn: "PlantID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
