using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428140000_AddConfigurationSchemaType")]
    public partial class AddConfigurationSchemaType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchemaType",
                table: "ConfigurationSchemas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchemaType",
                table: "ConfigurationSchemas");
        }
    }
}
