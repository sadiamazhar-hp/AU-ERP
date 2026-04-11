using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBPNumberRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BPNumberRanges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BPNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BPID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    EndNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPNumberRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_BPNumberRanges_BusinessPartnerMasterSamples_BPID",
                        column: x => x.BPID,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BPNumberRanges_BPID",
                table: "BPNumberRanges",
                column: "BPID");
        }
    }
}
