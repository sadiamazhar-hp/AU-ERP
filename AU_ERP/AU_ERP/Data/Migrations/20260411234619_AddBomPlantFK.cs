using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBomPlantFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Plant",
                table: "BomHeadersSamples",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_Plant",
                table: "BomHeadersSamples",
                column: "Plant");

            migrationBuilder.AddForeignKey(
                name: "FK_BomHeadersSamples_PlantsSamples_Plant",
                table: "BomHeadersSamples",
                column: "Plant",
                principalTable: "PlantsSamples",
                principalColumn: "PlantID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomHeadersSamples_PlantsSamples_Plant",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_Plant",
                table: "BomHeadersSamples");

            migrationBuilder.AlterColumn<string>(
                name: "Plant",
                table: "BomHeadersSamples",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
