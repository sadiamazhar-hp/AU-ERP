using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIntegrationAndProductionDocNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductionDocumentNumber",
                table: "ProductionOrders",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentIntegrations",
                columns: table => new
                {
                    DocumentIntegrationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIntegrations", x => x.DocumentIntegrationID);
                    table.ForeignKey(
                        name: "FK_DocumentIntegrations_DocumentTypes_DocumentTypeID",
                        column: x => x.DocumentTypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIntegrations_DocumentTypeID",
                table: "DocumentIntegrations",
                column: "DocumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIntegrations_ModuleKey",
                table: "DocumentIntegrations",
                column: "ModuleKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentIntegrations");

            migrationBuilder.DropColumn(
                name: "ProductionDocumentNumber",
                table: "ProductionOrders");
        }
    }
}
