using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class BpLookupsIntIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BPTypeNumberRanges_BPTypeSamples_BPType",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGrouping",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRole",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPType",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPGrouping",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPRole",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPType",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPTypeSamples",
                table: "BPTypeSamples");

            migrationBuilder.DropIndex(
                name: "IX_BPTypeNumberRanges_BPType",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPRoles",
                table: "BPRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPGroupings",
                table: "BPGroupings");

            migrationBuilder.DropColumn(
                name: "BPGrouping",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "BPRole",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "BPType",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "BPTypeID",
                table: "BPTypeSamples");

            migrationBuilder.DropColumn(
                name: "BPType",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropColumn(
                name: "RoleID",
                table: "BPRoles");

            migrationBuilder.DropColumn(
                name: "GroupID",
                table: "BPGroupings");

            migrationBuilder.AddColumn<int>(
                name: "BPGroupingId",
                table: "BusinessPartnerMasterSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BPRoleId",
                table: "BusinessPartnerMasterSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BPTypeId",
                table: "BusinessPartnerMasterSamples",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "BPTypeSamples",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "BPTypeId",
                table: "BPTypeNumberRanges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "BPRoles",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "RoleCode",
                table: "BPRoles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "BPGroupings",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.Sql(
                "UPDATE BPRoles SET RoleCode = CONCAT(N'R', CAST(Id AS nvarchar(20))) WHERE RoleCode = N''");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPTypeSamples",
                table: "BPTypeSamples",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPRoles",
                table: "BPRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPGroupings",
                table: "BPGroupings",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPGroupingId",
                table: "BusinessPartnerMasterSamples",
                column: "BPGroupingId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPRoleId",
                table: "BusinessPartnerMasterSamples",
                column: "BPRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPTypeId",
                table: "BusinessPartnerMasterSamples",
                column: "BPTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BPTypeNumberRanges_BPTypeId",
                table: "BPTypeNumberRanges",
                column: "BPTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BPRoles_RoleCode",
                table: "BPRoles",
                column: "RoleCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BPTypeNumberRanges_BPTypeSamples_BPTypeId",
                table: "BPTypeNumberRanges",
                column: "BPTypeId",
                principalTable: "BPTypeSamples",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGroupingId",
                table: "BusinessPartnerMasterSamples",
                column: "BPGroupingId",
                principalTable: "BPGroupings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRoleId",
                table: "BusinessPartnerMasterSamples",
                column: "BPRoleId",
                principalTable: "BPRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPTypeId",
                table: "BusinessPartnerMasterSamples",
                column: "BPTypeId",
                principalTable: "BPTypeSamples",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BPTypeNumberRanges_BPTypeSamples_BPTypeId",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGroupingId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRoleId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPTypeId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPGroupingId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPRoleId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropIndex(
                name: "IX_BusinessPartnerMasterSamples_BPTypeId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPTypeSamples",
                table: "BPTypeSamples");

            migrationBuilder.DropIndex(
                name: "IX_BPTypeNumberRanges_BPTypeId",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPRoles",
                table: "BPRoles");

            migrationBuilder.DropIndex(
                name: "IX_BPRoles_RoleCode",
                table: "BPRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BPGroupings",
                table: "BPGroupings");

            migrationBuilder.DropColumn(
                name: "BPGroupingId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "BPRoleId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "BPTypeId",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BPTypeSamples");

            migrationBuilder.DropColumn(
                name: "BPTypeId",
                table: "BPTypeNumberRanges");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BPRoles");

            migrationBuilder.DropColumn(
                name: "RoleCode",
                table: "BPRoles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BPGroupings");

            migrationBuilder.AddColumn<string>(
                name: "BPGrouping",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BPRole",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BPType",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BPTypeID",
                table: "BPTypeSamples",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BPType",
                table: "BPTypeNumberRanges",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleID",
                table: "BPRoles",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupID",
                table: "BPGroupings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPTypeSamples",
                table: "BPTypeSamples",
                column: "BPTypeID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPRoles",
                table: "BPRoles",
                column: "RoleID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BPGroupings",
                table: "BPGroupings",
                column: "GroupID");

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

            migrationBuilder.CreateIndex(
                name: "IX_BPTypeNumberRanges_BPType",
                table: "BPTypeNumberRanges",
                column: "BPType");

            migrationBuilder.AddForeignKey(
                name: "FK_BPTypeNumberRanges_BPTypeSamples_BPType",
                table: "BPTypeNumberRanges",
                column: "BPType",
                principalTable: "BPTypeSamples",
                principalColumn: "BPTypeID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGrouping",
                table: "BusinessPartnerMasterSamples",
                column: "BPGrouping",
                principalTable: "BPGroupings",
                principalColumn: "GroupID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRole",
                table: "BusinessPartnerMasterSamples",
                column: "BPRole",
                principalTable: "BPRoles",
                principalColumn: "RoleID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPType",
                table: "BusinessPartnerMasterSamples",
                column: "BPType",
                principalTable: "BPTypeSamples",
                principalColumn: "BPTypeID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
