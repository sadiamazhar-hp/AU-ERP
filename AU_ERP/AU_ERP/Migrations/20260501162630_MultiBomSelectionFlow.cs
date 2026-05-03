using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class MultiBomSelectionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedBomAlternative",
                table: "ProductionOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedBomId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedBomAlternative",
                table: "ProductionOrderLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedBomId",
                table: "ProductionOrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedBomAlternative",
                table: "GoodsIssueDocumentLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedBomId",
                table: "GoodsIssueDocumentLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternativeNo",
                table: "BomHeadersSamples",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ALT-1");

            migrationBuilder.AddColumn<string>(
                name: "BomUsage",
                table: "BomHeadersSamples",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultBom",
                table: "BomHeadersSamples",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BomHeadersSamples",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "BomHeadersSamples",
                type: "datetime2",
                nullable: true);
            
            migrationBuilder.Sql(@"
UPDATE [BomHeadersSamples]
SET [BomUsage] = CASE WHEN [BomUsage] IS NULL OR LTRIM(RTRIM([BomUsage])) = '' THEN 'Production' ELSE [BomUsage] END,
    [Status] = CASE WHEN [Status] IS NULL OR LTRIM(RTRIM([Status])) = '' THEN 'Active' ELSE [Status] END;");
            
            migrationBuilder.Sql(@"
;WITH Ranked AS (
    SELECT [BomID],
           ROW_NUMBER() OVER (
               PARTITION BY [BomMaterialNumber], [Plant]
               ORDER BY ISNULL([ValidFrom], '19000101') DESC, [BomID] DESC
           ) AS rn
    FROM [BomHeadersSamples]
    WHERE [BomMaterialNumber] IS NOT NULL AND [Plant] IS NOT NULL
)
UPDATE b
SET b.[AlternativeNo] = CONCAT('ALT-', CAST(r.rn AS nvarchar(20))),
    b.[IsDefaultBom] = CASE WHEN r.rn = 1 THEN 1 ELSE 0 END
FROM [BomHeadersSamples] b
INNER JOIN Ranked r ON r.[BomID] = b.[BomID];
");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SelectedBomId",
                table: "ProductionOrders",
                column: "SelectedBomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderLines_SelectedBomId",
                table: "ProductionOrderLines",
                column: "SelectedBomId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueDocumentLines_SelectedBomId",
                table: "GoodsIssueDocumentLines",
                column: "SelectedBomId");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples",
                columns: new[] { "BomMaterialNumber", "Plant", "BomUsage", "AlternativeNo" },
                unique: true,
                filter: "[BomMaterialNumber] IS NOT NULL AND [Plant] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_IsDefaultBom_Status",
                table: "BomHeadersSamples",
                columns: new[] { "BomMaterialNumber", "Plant", "IsDefaultBom", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsIssueDocumentLines_BomHeadersSamples_SelectedBomId",
                table: "GoodsIssueDocumentLines",
                column: "SelectedBomId",
                principalTable: "BomHeadersSamples",
                principalColumn: "BomID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderLines_BomHeadersSamples_SelectedBomId",
                table: "ProductionOrderLines",
                column: "SelectedBomId",
                principalTable: "BomHeadersSamples",
                principalColumn: "BomID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_BomHeadersSamples_SelectedBomId",
                table: "ProductionOrders",
                column: "SelectedBomId",
                principalTable: "BomHeadersSamples",
                principalColumn: "BomID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsIssueDocumentLines_BomHeadersSamples_SelectedBomId",
                table: "GoodsIssueDocumentLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderLines_BomHeadersSamples_SelectedBomId",
                table: "ProductionOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_BomHeadersSamples_SelectedBomId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SelectedBomId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderLines_SelectedBomId",
                table: "ProductionOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_GoodsIssueDocumentLines_SelectedBomId",
                table: "GoodsIssueDocumentLines");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_BomUsage_AlternativeNo",
                table: "BomHeadersSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber_Plant_IsDefaultBom_Status",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "SelectedBomAlternative",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SelectedBomId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SelectedBomAlternative",
                table: "ProductionOrderLines");

            migrationBuilder.DropColumn(
                name: "SelectedBomId",
                table: "ProductionOrderLines");

            migrationBuilder.DropColumn(
                name: "SelectedBomAlternative",
                table: "GoodsIssueDocumentLines");

            migrationBuilder.DropColumn(
                name: "SelectedBomId",
                table: "GoodsIssueDocumentLines");

            migrationBuilder.DropColumn(
                name: "AlternativeNo",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "BomUsage",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "IsDefaultBom",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BomHeadersSamples");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "BomHeadersSamples");
        }
    }
}
