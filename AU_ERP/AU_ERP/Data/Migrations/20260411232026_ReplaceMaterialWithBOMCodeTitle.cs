using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMaterialWithBOMCodeTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomHeadersSamples_CreateMaterialMaster_MaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_BomHeadersSamples_MaterialTypes_MaterialTypeCode",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_MaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_MaterialTypeCode",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "MaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "MaterialTypeCode",
                table: "BomHeadersSamples");

            migrationBuilder.AddColumn<string>(
                name: "BOMCode",
                table: "BomHeadersSamples",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BOMTitle",
                table: "BomHeadersSamples",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BOMCode",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "BOMTitle",
                table: "BomHeadersSamples");

            migrationBuilder.AddColumn<string>(
                name: "MaterialNumber",
                table: "BomHeadersSamples",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialTypeCode",
                table: "BomHeadersSamples",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_MaterialNumber",
                table: "BomHeadersSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_MaterialTypeCode",
                table: "BomHeadersSamples",
                column: "MaterialTypeCode");

            migrationBuilder.AddForeignKey(
                name: "FK_BomHeadersSamples_CreateMaterialMaster_MaterialNumber",
                table: "BomHeadersSamples",
                column: "MaterialNumber",
                principalTable: "CreateMaterialMaster",
                principalColumn: "MaterialNumber",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BomHeadersSamples_MaterialTypes_MaterialTypeCode",
                table: "BomHeadersSamples",
                column: "MaterialTypeCode",
                principalTable: "MaterialTypes",
                principalColumn: "MaterialTypeCode",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
