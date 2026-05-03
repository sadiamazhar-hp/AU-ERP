using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    public partial class MaterialTypeEnabledTabsConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnabledTabsJson",
                table: "MaterialTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.UpdateData(
                table: "MaterialTypes",
                keyColumn: "MaterialTypeCode",
                keyValue: "FERT",
                column: "EnabledTabsJson",
                value: "[\"Accounting\",\"BasicData\",\"MRP\",\"Sales\"]");

            migrationBuilder.UpdateData(
                table: "MaterialTypes",
                keyColumn: "MaterialTypeCode",
                keyValue: "HALB",
                column: "EnabledTabsJson",
                value: "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]");

            migrationBuilder.UpdateData(
                table: "MaterialTypes",
                keyColumn: "MaterialTypeCode",
                keyValue: "PACK",
                column: "EnabledTabsJson",
                value: "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]");

            migrationBuilder.UpdateData(
                table: "MaterialTypes",
                keyColumn: "MaterialTypeCode",
                keyValue: "ROH",
                column: "EnabledTabsJson",
                value: "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledTabsJson",
                table: "MaterialTypes");
        }
    }
}
