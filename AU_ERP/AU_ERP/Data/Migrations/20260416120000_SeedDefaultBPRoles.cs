using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416120000_SeedDefaultBPRoles")]
    public class SeedDefaultBPRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[BPRoles] WHERE [RoleCode] = N'FLCU00')
    INSERT INTO [dbo].[BPRoles] ([RoleCode], [RoleName]) VALUES (N'FLCU00', N'Basic');
IF NOT EXISTS (SELECT 1 FROM [dbo].[BPRoles] WHERE [RoleCode] = N'FLCU01')
    INSERT INTO [dbo].[BPRoles] ([RoleCode], [RoleName]) VALUES (N'FLCU01', N'Customer (Sales)');
IF NOT EXISTS (SELECT 1 FROM [dbo].[BPRoles] WHERE [RoleCode] = N'FLVN01')
    INSERT INTO [dbo].[BPRoles] ([RoleCode], [RoleName]) VALUES (N'FLVN01', N'Vendor');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [dbo].[BPRoles] WHERE [RoleCode] IN (N'FLCU00', N'FLCU01', N'FLVN01');
");
        }
    }
}
