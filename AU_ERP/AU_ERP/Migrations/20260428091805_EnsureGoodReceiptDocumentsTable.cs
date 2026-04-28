using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class EnsureGoodReceiptDocumentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GoodReceiptDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GoodReceiptDocuments](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT NOT NULL,
        [DocumentDate] DATE NOT NULL,
        [DocumentNumber] NVARCHAR(40) NOT NULL,
        [BatchNo] NVARCHAR(64) NOT NULL,
        [ProducedQty] DECIMAL(18,4) NOT NULL,
        [QtyFirstQuality] DECIMAL(18,4) NOT NULL,
        [QtySecondQuality] DECIMAL(18,4) NOT NULL,
        [QtyThirdQuality] DECIMAL(18,4) NOT NULL,
        [RejectedScrapQty] DECIMAL(18,4) NOT NULL,
        [IsPosted] BIT NOT NULL,
        [PostedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_GoodReceiptDocuments] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_GoodReceiptDocuments_ProductionOrders_ProductionOrderId]
            FOREIGN KEY([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_GoodReceiptDocuments_ProductionOrderId] ON [dbo].[GoodReceiptDocuments]([ProductionOrderId]);
    CREATE UNIQUE INDEX [IX_GoodReceiptDocuments_DocumentNumber] ON [dbo].[GoodReceiptDocuments]([DocumentNumber]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GoodReceiptDocuments]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[GoodReceiptDocuments];
END
");
        }
    }
}
