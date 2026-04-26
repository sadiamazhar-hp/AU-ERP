using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationSchemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationSchemaCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationSchemaId = table.Column<int>(type: "int", nullable: false),
                    ChargeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSchemaCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationSchemaCharges_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationSchemaCharges_ConfigurationSchemas_ConfigurationSchemaId",
                        column: x => x.ConfigurationSchemaId,
                        principalTable: "ConfigurationSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemaCharges_ChargeId",
                table: "ConfigurationSchemaCharges",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemaCharges_ConfigurationSchemaId_ChargeId",
                table: "ConfigurationSchemaCharges",
                columns: new[] { "ConfigurationSchemaId", "ChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemas_Title",
                table: "ConfigurationSchemas",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationSchemaCharges");

            migrationBuilder.DropTable(
                name: "ConfigurationSchemas");
        }
    }
}
