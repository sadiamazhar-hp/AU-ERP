using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416230000_MaterialGrProcessingUomAndSeedMaterialTypes")]
    public class MaterialGrProcessingUomAndSeedMaterialTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gr_Processing_UOM",
                table: "CreateMaterialMaster",
                type: "nvarchar(10)",
                nullable: true);

            migrationBuilder.Sql(@"
MERGE [dbo].[MaterialTypes] AS t
USING (VALUES
  (N'ROH', N'Raw Materials'),
  (N'HALB', N'Semi Products'),
  (N'FERT', N'Finished goods'),
  (N'VERP', N'Packaging Materials')
) AS s ([MaterialTypeCode], [Description])
ON t.[MaterialTypeCode] = s.[MaterialTypeCode]
WHEN MATCHED THEN UPDATE SET [Description] = s.[Description], [FieldReference] = NULL
WHEN NOT MATCHED THEN INSERT ([MaterialTypeCode], [Description], [FieldReference])
  VALUES (s.[MaterialTypeCode], s.[Description], NULL);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gr_Processing_UOM",
                table: "CreateMaterialMaster");
        }
    }
}
