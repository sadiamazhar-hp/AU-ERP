using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260412160000_SeedDefaultMaterialGroups")]
    public class SeedDefaultMaterialGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialGroups] WHERE [MaterialGroupCode] = N'PG01')
    INSERT INTO [dbo].[MaterialGroups] ([MaterialGroupCode], [Description], [AuthorizationGroup]) VALUES (N'PG01', N'Floor Tiles', NULL);
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialGroups] WHERE [MaterialGroupCode] = N'PG02')
    INSERT INTO [dbo].[MaterialGroups] ([MaterialGroupCode], [Description], [AuthorizationGroup]) VALUES (N'PG02', N'Wall Tiles', NULL);
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialGroups] WHERE [MaterialGroupCode] = N'PG03')
    INSERT INTO [dbo].[MaterialGroups] ([MaterialGroupCode], [Description], [AuthorizationGroup]) VALUES (N'PG03', N'Raw Materials', NULL);
IF NOT EXISTS (SELECT 1 FROM [dbo].[MaterialGroups] WHERE [MaterialGroupCode] = N'PG04')
    INSERT INTO [dbo].[MaterialGroups] ([MaterialGroupCode], [Description], [AuthorizationGroup]) VALUES (N'PG04', N'Packaging Materials', NULL);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [dbo].[MaterialGroups] WHERE [MaterialGroupCode] IN (N'PG01', N'PG02', N'PG03', N'PG04');
");
        }
    }
}
