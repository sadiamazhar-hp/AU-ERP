using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessPartnerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BPGroupings",
                columns: table => new
                {
                    GroupID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPGroupings", x => x.GroupID);
                });

            migrationBuilder.CreateTable(
                name: "BPRoles",
                columns: table => new
                {
                    RoleID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPRoles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "BPTypeSamples",
                columns: table => new
                {
                    BPTypeID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPTypeSamples", x => x.BPTypeID);
                });

            migrationBuilder.CreateTable(
                name: "BPTypeNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BPType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartNumber = table.Column<int>(type: "int", nullable: false),
                    EndNumber = table.Column<int>(type: "int", nullable: false),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPTypeNumberRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_BPTypeNumberRanges_BPTypeSamples_BPType",
                        column: x => x.BPType,
                        principalTable: "BPTypeSamples",
                        principalColumn: "BPTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPartnerMasterSamples",
                columns: table => new
                {
                    BPID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BPRole = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BPType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BPGrouping = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReconAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartnerMasterSamples", x => x.BPID);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGrouping",
                        column: x => x.BPGrouping,
                        principalTable: "BPGroupings",
                        principalColumn: "GroupID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRole",
                        column: x => x.BPRole,
                        principalTable: "BPRoles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPType",
                        column: x => x.BPType,
                        principalTable: "BPTypeSamples",
                        principalColumn: "BPTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BPNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BPID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartNumber = table.Column<int>(type: "int", nullable: false),
                    EndNumber = table.Column<int>(type: "int", nullable: false),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_BPTypeNumberRanges_BPType",
                table: "BPTypeNumberRanges",
                column: "BPType");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPGrouping",
                table: "BusinessPartnerMasterSamples",
                column: "BPGrouping");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPRole",
                table: "BusinessPartnerMasterSamples",
                column: "BPRole");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPType",
                table: "BusinessPartnerMasterSamples",
                column: "BPType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BPNumberRanges");

            migrationBuilder.DropTable(
                name: "BPTypeNumberRanges");

            migrationBuilder.DropTable(
                name: "BusinessPartnerMasterSamples");

            migrationBuilder.DropTable(
                name: "BPGroupings");

            migrationBuilder.DropTable(
                name: "BPRoles");

            migrationBuilder.DropTable(
                name: "BPTypeSamples");
        }
    }
}
