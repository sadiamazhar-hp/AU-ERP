using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutingPlantsAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeID);
                });

            migrationBuilder.CreateTable(
                name: "PlantsSamples",
                columns: table => new
                {
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantsSamples", x => x.PlantID);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: true),
                    FromNumber = table.Column<int>(type: "int", nullable: true),
                    ToNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_DocumentRanges_DocumentTypes_DocumentTypeID",
                        column: x => x.DocumentTypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutingHeadersSamples",
                columns: table => new
                {
                    RoutingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingHeadersSamples", x => x.RoutingID);
                    table.ForeignKey(
                        name: "FK_RoutingHeadersSamples_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingHeadersSamples_PlantsSamples_PlantID",
                        column: x => x.PlantID,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterMasterSamples",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCenterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailableCapacity = table.Column<int>(type: "int", nullable: true),
                    UtilizationPercentage = table.Column<int>(type: "int", nullable: true),
                    SetupTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SetupUOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MachineUOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaborTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LaborUOM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterMasterSamples", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WorkCenterMasterSamples_PlantsSamples_PlantID",
                        column: x => x.PlantID,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutingOperationsSamples",
                columns: table => new
                {
                    OpID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutingID = table.Column<int>(type: "int", nullable: true),
                    WorkCenterID = table.Column<int>(type: "int", nullable: true),
                    OperationSequence = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LaborTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UoM = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperationsSamples", x => x.OpID);
                    table.ForeignKey(
                        name: "FK_RoutingOperationsSamples_RoutingHeadersSamples_RoutingID",
                        column: x => x.RoutingID,
                        principalTable: "RoutingHeadersSamples",
                        principalColumn: "RoutingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutingOperationsSamples_WorkCenterMasterSamples_WorkCenterID",
                        column: x => x.WorkCenterID,
                        principalTable: "WorkCenterMasterSamples",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRanges_DocumentTypeID",
                table: "DocumentRanges",
                column: "DocumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingHeadersSamples_MaterialNumber",
                table: "RoutingHeadersSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingHeadersSamples_PlantID",
                table: "RoutingHeadersSamples",
                column: "PlantID");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_RoutingID",
                table: "RoutingOperationsSamples",
                column: "RoutingID");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_WorkCenterID",
                table: "RoutingOperationsSamples",
                column: "WorkCenterID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterMasterSamples_PlantID",
                table: "WorkCenterMasterSamples",
                column: "PlantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentRanges");

            migrationBuilder.DropTable(
                name: "RoutingOperationsSamples");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "RoutingHeadersSamples");

            migrationBuilder.DropTable(
                name: "WorkCenterMasterSamples");

            migrationBuilder.DropTable(
                name: "PlantsSamples");
        }
    }
}
