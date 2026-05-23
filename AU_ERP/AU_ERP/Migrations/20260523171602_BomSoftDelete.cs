using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class BomSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "BomHeadersSamples",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BomHeadersSamples",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples",
                column: "BomMaterialNumber",
                unique: true,
                filter: "[IsDefaultBom] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples",
                columns: new[] { "BomMaterialNumber", "Plant", "BomUsage", "AlternativeNo" },
                unique: true,
                filter: "[BomMaterialNumber] IS NOT NULL AND [Plant] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples",
                column: "BomMaterialNumber",
                unique: true,
                filter: "[IsDefaultBom] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples",
                columns: new[] { "BomMaterialNumber", "Plant", "BomUsage", "AlternativeNo" },
                unique: true,
                filter: "[BomMaterialNumber] IS NOT NULL AND [Plant] IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BomHeadersSamples");
        }
    }
}
