using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class UomFkBomWorkCentreRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PurchasingGroupCode",
                table: "CreateMaterialMaster",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UomId",
                table: "WorkCenterMasterSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UomId",
                table: "BomItemsSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UomId",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: true);

            // Backfill work centre UOM from legacy string columns (Setup → Machine → Labor).
            migrationBuilder.Sql(@"
UPDATE wc SET [UomId] = u.[Id]
FROM [WorkCenterMasterSamples] AS wc
INNER JOIN [UnitOfMeasurements] AS u ON UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) = UPPER(LTRIM(RTRIM(ISNULL(wc.[SetupUOM], N''))))
WHERE NULLIF(LTRIM(RTRIM(ISNULL(wc.[SetupUOM], N''))), N'') IS NOT NULL;

UPDATE wc SET [UomId] = u.[Id]
FROM [WorkCenterMasterSamples] AS wc
INNER JOIN [UnitOfMeasurements] AS u ON UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) = UPPER(LTRIM(RTRIM(ISNULL(wc.[MachineUOM], N''))))
WHERE wc.[UomId] IS NULL AND NULLIF(LTRIM(RTRIM(ISNULL(wc.[MachineUOM], N''))), N'') IS NOT NULL;

UPDATE wc SET [UomId] = u.[Id]
FROM [WorkCenterMasterSamples] AS wc
INNER JOIN [UnitOfMeasurements] AS u ON UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) = UPPER(LTRIM(RTRIM(ISNULL(wc.[LaborUOM], N''))))
WHERE wc.[UomId] IS NULL AND NULLIF(LTRIM(RTRIM(ISNULL(wc.[LaborUOM], N''))), N'') IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE bi SET [UomId] = u.[Id]
FROM [BomItemsSamples] AS bi
INNER JOIN [UnitOfMeasurements] AS u ON UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) = UPPER(LTRIM(RTRIM(ISNULL(bi.[UoM], N''))))
WHERE NULLIF(LTRIM(RTRIM(ISNULL(bi.[UoM], N''))), N'') IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ro SET [UomId] = wc.[UomId]
FROM [RoutingOperationsSamples] AS ro
INNER JOIN [WorkCenterMasterSamples] AS wc ON wc.[ID] = ro.[WorkCenterID]
WHERE wc.[UomId] IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ro SET [UomId] = u.[Id]
FROM [RoutingOperationsSamples] AS ro
INNER JOIN [UnitOfMeasurements] AS u ON UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) = UPPER(LTRIM(RTRIM(ISNULL(ro.[UoM], N''))))
WHERE ro.[UomId] IS NULL AND NULLIF(LTRIM(RTRIM(ISNULL(ro.[UoM], N''))), N'') IS NOT NULL;
");

            migrationBuilder.DropColumn(
                name: "LaborUOM",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "MachineUOM",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "SetupUOM",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "UoM",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropColumn(
                name: "UoM",
                table: "BomItemsSamples");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterMasterSamples_UomId",
                table: "WorkCenterMasterSamples",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_UomId",
                table: "RoutingOperationsSamples",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_UomId",
                table: "BomItemsSamples",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_BomItemsSamples_UnitOfMeasurements_UomId",
                table: "BomItemsSamples",
                column: "UomId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoutingOperationsSamples_UnitOfMeasurements_UomId",
                table: "RoutingOperationsSamples",
                column: "UomId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkCenterMasterSamples_UnitOfMeasurements_UomId",
                table: "WorkCenterMasterSamples",
                column: "UomId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomItemsSamples_UnitOfMeasurements_UomId",
                table: "BomItemsSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutingOperationsSamples_UnitOfMeasurements_UomId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkCenterMasterSamples_UnitOfMeasurements_UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_WorkCenterMasterSamples_UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_RoutingOperationsSamples_UomId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropIndex(
                name: "IX_BomItemsSamples_UomId",
                table: "BomItemsSamples");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "BomItemsSamples");

            migrationBuilder.AddColumn<string>(
                name: "LaborUOM",
                table: "WorkCenterMasterSamples",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineUOM",
                table: "WorkCenterMasterSamples",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SetupUOM",
                table: "WorkCenterMasterSamples",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UoM",
                table: "RoutingOperationsSamples",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UoM",
                table: "BomItemsSamples",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PurchasingGroupCode",
                table: "CreateMaterialMaster",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
