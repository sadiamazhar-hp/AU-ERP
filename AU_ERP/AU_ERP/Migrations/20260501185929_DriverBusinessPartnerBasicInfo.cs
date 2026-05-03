using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class DriverBusinessPartnerBasicInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesGoodsIssueDocumentLines_SalesOrderItems_SalesOrderItemId",
                table: "SalesGoodsIssueDocumentLines");

            migrationBuilder.AddColumn<string>(
                name: "CNIC",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BusinessPartnerMasterSamples",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenceNo",
                table: "BusinessPartnerMasterSamples",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.InsertData(
                table: "BPRoles",
                columns: new[] { "Id", "RoleCode", "RoleName" },
                values: new object[] { 4, "FLDR01", "Driver" });

            migrationBuilder.InsertData(
                table: "BPTypeSamples",
                columns: new[] { "Id", "CreatedAt", "IsActive", "TypeName" },
                values: new object[] { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Driver" });

            migrationBuilder.AddForeignKey(
                name: "FK_SalesGoodsIssueDocumentLines_SalesOrderItems_SalesOrderItemId",
                table: "SalesGoodsIssueDocumentLines",
                column: "SalesOrderItemId",
                principalTable: "SalesOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesGoodsIssueDocumentLines_SalesOrderItems_SalesOrderItemId",
                table: "SalesGoodsIssueDocumentLines");

            migrationBuilder.DeleteData(
                table: "BPRoles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "BPTypeSamples",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "CNIC",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.DropColumn(
                name: "LicenceNo",
                table: "BusinessPartnerMasterSamples");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesGoodsIssueDocumentLines_SalesOrderItems_SalesOrderItemId",
                table: "SalesGoodsIssueDocumentLines",
                column: "SalesOrderItemId",
                principalTable: "SalesOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
