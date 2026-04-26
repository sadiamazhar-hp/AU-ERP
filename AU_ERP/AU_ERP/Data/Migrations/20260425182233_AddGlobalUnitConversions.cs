using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalUnitConversions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalUnitConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseUnitId = table.Column<int>(type: "int", nullable: false),
                    AltUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalUnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalUnitConversions_UnitOfMeasurements_AltUnitId",
                        column: x => x.AltUnitId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GlobalUnitConversions_UnitOfMeasurements_BaseUnitId",
                        column: x => x.BaseUnitId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUnitConversions_AltUnitId",
                table: "GlobalUnitConversions",
                column: "AltUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUnitConversions_BaseUnitId_AltUnitId",
                table: "GlobalUnitConversions",
                columns: new[] { "BaseUnitId", "AltUnitId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalUnitConversions");
        }
    }
}
