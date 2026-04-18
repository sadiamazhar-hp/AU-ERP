using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417140000_AddProductionOrderStageProgress")]
    public partial class AddProductionOrderStageProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReleasedRoutingId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOrderStageProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    RoutingOperationHeaderId = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    StageTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlannedHours = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    InputQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    OutputQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    WastageQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    WastageReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkerOperator = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StageStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderStageProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageProgresses_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageProgresses_RoutingOperationHeadersSamples_RoutingOperationHeaderId",
                        column: x => x.RoutingOperationHeaderId,
                        principalTable: "RoutingOperationHeadersSamples",
                        principalColumn: "OperationHeaderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageProgresses_ProductionOrderId",
                table: "ProductionOrderStageProgresses",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageProgresses_RoutingOperationHeaderId",
                table: "ProductionOrderStageProgresses",
                column: "RoutingOperationHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ReleasedRoutingId",
                table: "ProductionOrders",
                column: "ReleasedRoutingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_RoutingHeadersSamples_ReleasedRoutingId",
                table: "ProductionOrders",
                column: "ReleasedRoutingId",
                principalTable: "RoutingHeadersSamples",
                principalColumn: "RoutingID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_RoutingHeadersSamples_ReleasedRoutingId",
                table: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "ProductionOrderStageProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_ReleasedRoutingId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "ReleasedRoutingId",
                table: "ProductionOrders");
        }
    }
}
