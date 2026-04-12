using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260412183000_UnitConversionAltUnitIdFk")]
    public class UnitConversionAltUnitIdFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AltUnitId",
                table: "UnitConversions",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE uc SET uc.AltUnitId = u.Id
FROM [dbo].[UnitConversions] AS uc
INNER JOIN [dbo].[UnitOfMeasurements] AS u
    ON u.[Code] IS NOT NULL AND LTRIM(RTRIM(u.[Code])) = LTRIM(RTRIM(uc.[AltUnitCode]));
");

            migrationBuilder.Sql(@"
DELETE FROM [dbo].[UnitConversions] WHERE [AltUnitId] IS NULL;
");

            migrationBuilder.DropColumn(
                name: "AltUnitCode",
                table: "UnitConversions");

            migrationBuilder.AlterColumn<int>(
                name: "AltUnitId",
                table: "UnitConversions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_AltUnitId",
                table: "UnitConversions",
                column: "AltUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitConversions_UnitOfMeasurements_AltUnitId",
                table: "UnitConversions",
                column: "AltUnitId",
                principalTable: "UnitOfMeasurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnitConversions_UnitOfMeasurements_AltUnitId",
                table: "UnitConversions");

            migrationBuilder.DropIndex(
                name: "IX_UnitConversions_AltUnitId",
                table: "UnitConversions");

            migrationBuilder.AddColumn<string>(
                name: "AltUnitCode",
                table: "UnitConversions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE uc SET uc.AltUnitCode = ISNULL(u.[Code], CAST(uc.AltUnitId AS nvarchar(20)))
FROM [dbo].[UnitConversions] AS uc
LEFT JOIN [dbo].[UnitOfMeasurements] AS u ON u.[Id] = uc.[AltUnitId];
");

            migrationBuilder.AlterColumn<string>(
                name: "AltUnitCode",
                table: "UnitConversions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "AltUnitId",
                table: "UnitConversions");
        }
    }
}
