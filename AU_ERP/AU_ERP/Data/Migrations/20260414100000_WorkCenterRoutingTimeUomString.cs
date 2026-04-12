using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260414100000_WorkCenterRoutingTimeUomString")]
    public partial class WorkCenterRoutingTimeUomString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoutingOperationsSamples_UnitOfMeasurements_UomId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkCenterMasterSamples_UnitOfMeasurements_UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_RoutingOperationsSamples_UomId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropIndex(
                name: "IX_WorkCenterMasterSamples_UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.AddColumn<string>(
                name: "TimeUom",
                table: "WorkCenterMasterSamples",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeUom",
                table: "RoutingOperationsSamples",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE wc SET [TimeUom] = CASE
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'MIN', N'M', N'MINUTE', N'MINUTES') THEN N'Min'
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'HR', N'H', N'HOUR', N'HOURS', N'HRS') THEN N'Hr'
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'DAY', N'D', N'DAYS') THEN N'Day'
  ELSE NULL END
FROM [WorkCenterMasterSamples] AS wc
INNER JOIN [UnitOfMeasurements] AS u ON u.[Id] = wc.[UomId]
WHERE wc.[UomId] IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ro SET [TimeUom] = wc.[TimeUom]
FROM [RoutingOperationsSamples] AS ro
INNER JOIN [WorkCenterMasterSamples] AS wc ON wc.[ID] = ro.[WorkCenterID]
WHERE wc.[TimeUom] IS NOT NULL;
");

            migrationBuilder.Sql(@"
UPDATE ro SET [TimeUom] = CASE
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'MIN', N'M', N'MINUTE', N'MINUTES') THEN N'Min'
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'HR', N'H', N'HOUR', N'HOURS', N'HRS') THEN N'Hr'
  WHEN UPPER(LTRIM(RTRIM(ISNULL(u.[Code], N'')))) IN (N'DAY', N'D', N'DAYS') THEN N'Day'
  ELSE NULL END
FROM [RoutingOperationsSamples] AS ro
INNER JOIN [UnitOfMeasurements] AS u ON u.[Id] = ro.[UomId]
WHERE ro.[UomId] IS NOT NULL AND NULLIF(LTRIM(RTRIM(ISNULL(ro.[TimeUom], N''))), N'') IS NULL;
");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "RoutingOperationsSamples");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UomId",
                table: "WorkCenterMasterSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UomId",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "TimeUom",
                table: "WorkCenterMasterSamples");

            migrationBuilder.DropColumn(
                name: "TimeUom",
                table: "RoutingOperationsSamples");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterMasterSamples_UomId",
                table: "WorkCenterMasterSamples",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_UomId",
                table: "RoutingOperationsSamples",
                column: "UomId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkCenterMasterSamples_UnitOfMeasurements_UomId",
                table: "WorkCenterMasterSamples",
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
        }
    }
}
