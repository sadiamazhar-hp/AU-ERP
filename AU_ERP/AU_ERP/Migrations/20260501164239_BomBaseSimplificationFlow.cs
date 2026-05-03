using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class BomBaseSimplificationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE b
SET b.HeaderMaterialTypeCode = 'FERT'
FROM BomHeadersSamples b
WHERE ISNULL(LTRIM(RTRIM(b.HeaderMaterialTypeCode)), '') <> 'FERT';
");

            migrationBuilder.Sql(@"
UPDATE b
SET b.AlternativeNo = ISNULL(NULLIF(LTRIM(RTRIM(b.BOMCode)), ''), CONCAT('BOM-', CAST(b.BomID AS nvarchar(20))))
FROM BomHeadersSamples b
WHERE ISNULL(LTRIM(RTRIM(b.AlternativeNo)), '') = ''
   OR b.AlternativeNo = 'AUTO'
   OR b.AlternativeNo = 'ALT-1';
");

            migrationBuilder.Sql(@"
;WITH cte AS (
    SELECT
        b.BomID,
        rn = ROW_NUMBER() OVER (
            PARTITION BY b.BomMaterialNumber
            ORDER BY
                CASE WHEN b.IsDefaultBom = 1 THEN 0 ELSE 1 END,
                CASE WHEN b.Status = 'Active' THEN 0 ELSE 1 END,
                ISNULL(b.ValidFrom, '19000101') DESC,
                b.BomID DESC
        )
    FROM BomHeadersSamples b
    WHERE ISNULL(LTRIM(RTRIM(b.BomMaterialNumber)), '') <> ''
)
UPDATE b
SET b.IsDefaultBom = CASE WHEN c.rn = 1 THEN 1 ELSE 0 END
FROM BomHeadersSamples b
JOIN cte c ON c.BomID = b.BomID;
");

            migrationBuilder.AlterColumn<string>(
                name: "AlternativeNo",
                table: "BomHeadersSamples",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AUTO",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ALT-1");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples",
                column: "BomMaterialNumber",
                unique: true,
                filter: "[IsDefaultBom] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BomHeadersSamples_BomMaterialNumber",
                table: "BomHeadersSamples");

            migrationBuilder.AlterColumn<string>(
                name: "AlternativeNo",
                table: "BomHeadersSamples",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ALT-1",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "AUTO");
        }
    }
}
