using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416200000_UpdateBpRoleFlcu00ToBasic")]
    public class UpdateBpRoleFlcu00ToBasic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [dbo].[BPRoles] SET [RoleName] = N'Basic' WHERE [RoleCode] = N'FLCU00';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [dbo].[BPRoles] SET [RoleName] = N'Customer (FI)' WHERE [RoleCode] = N'FLCU00';
");
        }
    }
}
