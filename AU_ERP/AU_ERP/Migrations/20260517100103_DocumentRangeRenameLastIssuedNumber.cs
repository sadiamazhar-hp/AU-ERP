using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class DocumentRangeRenameLastIssuedNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE DocumentRanges
                SET CurrentNumber = FromNumber - 1
                WHERE FromNumber IS NOT NULL
                  AND (CurrentNumber IS NULL OR CurrentNumber = 0);
                """);

            migrationBuilder.RenameColumn(
                name: "CurrentNumber",
                table: "DocumentRanges",
                newName: "LastIssuedNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastIssuedNumber",
                table: "DocumentRanges",
                newName: "CurrentNumber");
        }
    }
}
