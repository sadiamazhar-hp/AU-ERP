using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovementNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "date", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FromPlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ToPlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    QuantityMoved = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    QuantityUomId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_PlantsSamples_FromPlantId",
                        column: x => x.FromPlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_PlantsSamples_ToPlantId",
                        column: x => x.ToPlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_FromPlantId",
                table: "StockMovements",
                column: "FromPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MaterialNumber",
                table: "StockMovements",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementNumber",
                table: "StockMovements",
                column: "MovementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_QuantityUomId",
                table: "StockMovements",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ToPlantId",
                table: "StockMovements",
                column: "ToPlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovements");
        }
    }
}
