using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class MaterialGroupBomAndNumberRangeFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BOMLevelsSamples",
                columns: table => new
                {
                    LevelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMLevelsSamples", x => x.LevelID);
                });

            migrationBuilder.CreateTable(
                name: "MaterialGroups",
                columns: table => new
                {
                    MaterialGroupCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizationGroup = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialGroups", x => x.MaterialGroupCode);
                });

            migrationBuilder.CreateTable(
                name: "MaterialTypes",
                columns: table => new
                {
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialTypes", x => x.MaterialTypeCode);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AltUnitCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numerator = table.Column<float>(type: "real", nullable: false),
                    Denominator = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreateMaterialMaster",
                columns: table => new
                {
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IndustrySectorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseUnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialGroupCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Division = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveringPlantCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemCategoryGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchasingGroupCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrProcessingTime = table.Column<int>(type: "int", nullable: true),
                    MrpTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcurementTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategyGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailabilityCheckCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValuationClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreateMaterialMaster", x => x.MaterialNumber);
                    table.ForeignKey(
                        name: "FK_CreateMaterialMaster_MaterialGroups_MaterialGroupCode",
                        column: x => x.MaterialGroupCode,
                        principalTable: "MaterialGroups",
                        principalColumn: "MaterialGroupCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreateMaterialMaster_MaterialTypes_MaterialTypeCode",
                        column: x => x.MaterialTypeCode,
                        principalTable: "MaterialTypes",
                        principalColumn: "MaterialTypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FromNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsExternal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialNumberRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode",
                        column: x => x.MaterialTypeCode,
                        principalTable: "MaterialTypes",
                        principalColumn: "MaterialTypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomHeadersSamples",
                columns: table => new
                {
                    BomID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BLevel = table.Column<int>(type: "int", nullable: true),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AlternativeBOM = table.Column<int>(type: "int", nullable: true),
                    Plant = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaseQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomHeadersSamples", x => x.BomID);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_BOMLevelsSamples_BLevel",
                        column: x => x.BLevel,
                        principalTable: "BOMLevelsSamples",
                        principalColumn: "LevelID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_BomHeadersSamples_AlternativeBOM",
                        column: x => x.AlternativeBOM,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_MaterialTypes_MaterialTypeCode",
                        column: x => x.MaterialTypeCode,
                        principalTable: "MaterialTypes",
                        principalColumn: "MaterialTypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomItemsSamples",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BomID = table.Column<int>(type: "int", nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UoM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScrapPercentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomItemsSamples", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_BomItemsSamples_BomHeadersSamples_BomID",
                        column: x => x.BomID,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomItemsSamples_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_AlternativeBOM",
                table: "BomHeadersSamples",
                column: "AlternativeBOM");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BLevel",
                table: "BomHeadersSamples",
                column: "BLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_MaterialNumber",
                table: "BomHeadersSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_MaterialTypeCode",
                table: "BomHeadersSamples",
                column: "MaterialTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_BomID",
                table: "BomItemsSamples",
                column: "BomID");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_MaterialNumber",
                table: "BomItemsSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CreateMaterialMaster_MaterialGroupCode",
                table: "CreateMaterialMaster",
                column: "MaterialGroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_CreateMaterialMaster_MaterialTypeCode",
                table: "CreateMaterialMaster",
                column: "MaterialTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialNumberRanges_MaterialTypeCode",
                table: "MaterialNumberRanges",
                column: "MaterialTypeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BomItemsSamples");

            migrationBuilder.DropTable(
                name: "MaterialNumberRanges");

            migrationBuilder.DropTable(
                name: "UnitConversions");

            migrationBuilder.DropTable(
                name: "BomHeadersSamples");

            migrationBuilder.DropTable(
                name: "BOMLevelsSamples");

            migrationBuilder.DropTable(
                name: "CreateMaterialMaster");

            migrationBuilder.DropTable(
                name: "MaterialGroups");

            migrationBuilder.DropTable(
                name: "MaterialTypes");
        }
    }
}
