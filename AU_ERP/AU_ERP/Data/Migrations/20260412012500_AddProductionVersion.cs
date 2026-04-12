using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BomId = table.Column<int>(type: "int", nullable: true),
                    RoutingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_BomHeadersSamples_BomId",
                        column: x => x.BomId,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_RoutingHeadersSamples_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "RoutingHeadersSamples",
                        principalColumn: "RoutingID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_BomId",
                table: "ProductionVersions",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_PlantId",
                table: "ProductionVersions",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_RoutingId",
                table: "ProductionVersions",
                column: "RoutingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionVersions");
        }
    }
}
