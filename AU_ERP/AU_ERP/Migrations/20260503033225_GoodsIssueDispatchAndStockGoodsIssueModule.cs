using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class GoodsIssueDispatchAndStockGoodsIssueModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchSentAt",
                table: "SalesGoodsIssueDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchSentByUserId",
                table: "SalesGoodsIssueDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchStatus",
                table: "SalesGoodsIssueDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchSentAt",
                table: "GoodsIssueDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchSentByUserId",
                table: "GoodsIssueDocuments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchStatus",
                table: "GoodsIssueDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            // Existing documents: treat dispatch as already sent so receipt / legacy flows keep working.
            migrationBuilder.Sql("""
                UPDATE GoodsIssueDocuments
                SET DispatchStatus = N'Sent',
                    DispatchSentAt = COALESCE(CompletedAt, CreatedAt)
                WHERE DispatchStatus = N'Pending';

                UPDATE SalesGoodsIssueDocuments
                SET DispatchStatus = N'Sent',
                    DispatchSentAt = COALESCE(ReceivedAt, CreatedAt)
                WHERE DispatchStatus = N'Pending';
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM DocumentIntegrations WHERE ModuleKey = N'StockGoodsIssue')
                BEGIN
                    INSERT INTO DocumentTypes (DocCode, Description)
                    VALUES (N'SGI', N'Stock Good Issue');
                    DECLARE @dt INT = CAST(SCOPE_IDENTITY() AS INT);
                    INSERT INTO DocumentRanges (DocumentTypeID, FromNumber, ToNumber, CurrentNumber)
                    VALUES (@dt, 1, 999999, NULL);
                    INSERT INTO DocumentIntegrations (ModuleKey, DocumentTypeID, IsActive, CreatedAt)
                    VALUES (N'StockGoodsIssue', @dt, 1, SYSUTCDATETIME());
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @dtId INT;
                SELECT @dtId = DocumentTypeID FROM DocumentIntegrations WHERE ModuleKey = N'StockGoodsIssue';
                IF @dtId IS NOT NULL
                BEGIN
                    DELETE FROM DocumentIntegrations WHERE ModuleKey = N'StockGoodsIssue';
                    DELETE FROM DocumentRanges WHERE DocumentTypeID = @dtId;
                    DELETE FROM DocumentTypes WHERE DocumentTypeID = @dtId;
                END
                """);

            migrationBuilder.DropColumn(
                name: "DispatchSentAt",
                table: "SalesGoodsIssueDocuments");

            migrationBuilder.DropColumn(
                name: "DispatchSentByUserId",
                table: "SalesGoodsIssueDocuments");

            migrationBuilder.DropColumn(
                name: "DispatchStatus",
                table: "SalesGoodsIssueDocuments");

            migrationBuilder.DropColumn(
                name: "DispatchSentAt",
                table: "GoodsIssueDocuments");

            migrationBuilder.DropColumn(
                name: "DispatchSentByUserId",
                table: "GoodsIssueDocuments");

            migrationBuilder.DropColumn(
                name: "DispatchStatus",
                table: "GoodsIssueDocuments");
        }
    }
}
