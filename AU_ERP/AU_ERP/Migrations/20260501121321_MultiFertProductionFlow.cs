using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class MultiFertProductionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsProduceBatches_ProductionOrderId' AND object_id = OBJECT_ID('GoodsProduceBatches')) DROP INDEX [IX_GoodsProduceBatches_ProductionOrderId] ON [GoodsProduceBatches];");

            migrationBuilder.Sql("IF COL_LENGTH('ProductionOrderStageProgresses','ProductionOrderLineId') IS NULL ALTER TABLE [ProductionOrderStageProgresses] ADD [ProductionOrderLineId] int NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsProduceBatches','MaterialNumber') IS NULL ALTER TABLE [GoodsProduceBatches] ADD [MaterialNumber] nvarchar(450) NOT NULL CONSTRAINT DF_GPB_MaterialNumber DEFAULT('');");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsProduceBatches','ProductionOrderLineId') IS NULL ALTER TABLE [GoodsProduceBatches] ADD [ProductionOrderLineId] int NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsProduceBatches','UomId') IS NULL ALTER TABLE [GoodsProduceBatches] ADD [UomId] int NOT NULL CONSTRAINT DF_GPB_UomId DEFAULT(0);");
            migrationBuilder.Sql("IF COL_LENGTH('GoodReceiptDocuments','DraftLinesJson') IS NULL ALTER TABLE [GoodReceiptDocuments] ADD [DraftLinesJson] nvarchar(max) NULL;");

            migrationBuilder.CreateTable(
                name: "ProductionOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlannedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UomId = table.Column<int>(type: "int", nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderLines_UnitOfMeasurements_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("IF COL_LENGTH('GoodsIssueDocumentLines','ProductionOrderLineId') IS NULL ALTER TABLE [GoodsIssueDocumentLines] ADD [ProductionOrderLineId] int NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsIssueDocumentLines','FertMaterialNumber') IS NULL ALTER TABLE [GoodsIssueDocumentLines] ADD [FertMaterialNumber] nvarchar(450) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsIssueDocumentLines','FertMaterialDescription') IS NULL ALTER TABLE [GoodsIssueDocumentLines] ADD [FertMaterialDescription] nvarchar(500) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsIssueDocumentLines','IssuedQty') IS NULL ALTER TABLE [GoodsIssueDocumentLines] ADD [IssuedQty] decimal(18,4) NOT NULL CONSTRAINT DF_GIDL_IssuedQty DEFAULT(0);");
            migrationBuilder.Sql("IF COL_LENGTH('GoodsIssueDocumentLines','RemainingQty') IS NULL ALTER TABLE [GoodsIssueDocumentLines] ADD [RemainingQty] decimal(18,4) NOT NULL CONSTRAINT DF_GIDL_RemainingQty DEFAULT(0);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderStageProgresses_ProductionOrderLineId' AND object_id = OBJECT_ID('ProductionOrderStageProgresses')) CREATE INDEX [IX_ProductionOrderStageProgresses_ProductionOrderLineId] ON [ProductionOrderStageProgresses] ([ProductionOrderLineId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsProduceBatches_MaterialNumber' AND object_id = OBJECT_ID('GoodsProduceBatches')) CREATE INDEX [IX_GoodsProduceBatches_MaterialNumber] ON [GoodsProduceBatches] ([MaterialNumber]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsProduceBatches_ProductionOrderId' AND object_id = OBJECT_ID('GoodsProduceBatches')) CREATE INDEX [IX_GoodsProduceBatches_ProductionOrderId] ON [GoodsProduceBatches] ([ProductionOrderId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsProduceBatches_ProductionOrderLineId' AND object_id = OBJECT_ID('GoodsProduceBatches')) CREATE INDEX [IX_GoodsProduceBatches_ProductionOrderLineId] ON [GoodsProduceBatches] ([ProductionOrderLineId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsProduceBatches_UomId' AND object_id = OBJECT_ID('GoodsProduceBatches')) CREATE INDEX [IX_GoodsProduceBatches_UomId] ON [GoodsProduceBatches] ([UomId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsIssueDocumentLines_FertMaterialNumber' AND object_id = OBJECT_ID('GoodsIssueDocumentLines')) CREATE INDEX [IX_GoodsIssueDocumentLines_FertMaterialNumber] ON [GoodsIssueDocumentLines] ([FertMaterialNumber]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsIssueDocumentLines_GoodsIssueDocumentId' AND object_id = OBJECT_ID('GoodsIssueDocumentLines')) CREATE INDEX [IX_GoodsIssueDocumentLines_GoodsIssueDocumentId] ON [GoodsIssueDocumentLines] ([GoodsIssueDocumentId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsIssueDocumentLines_MaterialNumber' AND object_id = OBJECT_ID('GoodsIssueDocumentLines')) CREATE INDEX [IX_GoodsIssueDocumentLines_MaterialNumber] ON [GoodsIssueDocumentLines] ([MaterialNumber]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsIssueDocumentLines_ProductionOrderLineId' AND object_id = OBJECT_ID('GoodsIssueDocumentLines')) CREATE INDEX [IX_GoodsIssueDocumentLines_ProductionOrderLineId] ON [GoodsIssueDocumentLines] ([ProductionOrderLineId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GoodsIssueDocumentLines_RequiredUomId' AND object_id = OBJECT_ID('GoodsIssueDocumentLines')) CREATE INDEX [IX_GoodsIssueDocumentLines_RequiredUomId] ON [GoodsIssueDocumentLines] ([RequiredUomId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderLines_MaterialNumber' AND object_id = OBJECT_ID('ProductionOrderLines')) CREATE INDEX [IX_ProductionOrderLines_MaterialNumber] ON [ProductionOrderLines] ([MaterialNumber]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderLines_PlantId' AND object_id = OBJECT_ID('ProductionOrderLines')) CREATE INDEX [IX_ProductionOrderLines_PlantId] ON [ProductionOrderLines] ([PlantId]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderLines_ProductionOrderId_LineNo' AND object_id = OBJECT_ID('ProductionOrderLines')) CREATE UNIQUE INDEX [IX_ProductionOrderLines_ProductionOrderId_LineNo] ON [ProductionOrderLines] ([ProductionOrderId], [LineNo]);");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionOrderLines_UomId' AND object_id = OBJECT_ID('ProductionOrderLines')) CREATE INDEX [IX_ProductionOrderLines_UomId] ON [ProductionOrderLines] ([UomId]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GoodsIssueDocumentLines_CreateMaterialMaster_FertMaterialNumber') ALTER TABLE [GoodsIssueDocumentLines] ADD CONSTRAINT [FK_GoodsIssueDocumentLines_CreateMaterialMaster_FertMaterialNumber] FOREIGN KEY ([FertMaterialNumber]) REFERENCES [CreateMaterialMaster]([MaterialNumber]) ON DELETE NO ACTION;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GoodsIssueDocumentLines_ProductionOrderLines_ProductionOrderLineId') ALTER TABLE [GoodsIssueDocumentLines] ADD CONSTRAINT [FK_GoodsIssueDocumentLines_ProductionOrderLines_ProductionOrderLineId] FOREIGN KEY ([ProductionOrderLineId]) REFERENCES [ProductionOrderLines]([Id]) ON DELETE SET NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsProduceBatches_CreateMaterialMaster_MaterialNumber",
                table: "GoodsProduceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsProduceBatches_ProductionOrderLines_ProductionOrderLineId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_GoodsProduceBatches_UnitOfMeasurements_UomId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderStageProgresses_ProductionOrderLines_ProductionOrderLineId",
                table: "ProductionOrderStageProgresses");

            migrationBuilder.DropTable(
                name: "GoodsIssueDocumentLines");

            migrationBuilder.DropTable(
                name: "GoodsIssueDocuments");

            migrationBuilder.DropTable(
                name: "ProductionOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrderStageProgresses_ProductionOrderLineId",
                table: "ProductionOrderStageProgresses");

            migrationBuilder.DropIndex(
                name: "IX_GoodsProduceBatches_MaterialNumber",
                table: "GoodsProduceBatches");

            migrationBuilder.DropIndex(
                name: "IX_GoodsProduceBatches_ProductionOrderId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropIndex(
                name: "IX_GoodsProduceBatches_ProductionOrderLineId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropIndex(
                name: "IX_GoodsProduceBatches_UomId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropColumn(
                name: "ProductionOrderLineId",
                table: "ProductionOrderStageProgresses");

            migrationBuilder.DropColumn(
                name: "MaterialNumber",
                table: "GoodsProduceBatches");

            migrationBuilder.DropColumn(
                name: "ProductionOrderLineId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropColumn(
                name: "UomId",
                table: "GoodsProduceBatches");

            migrationBuilder.DropColumn(
                name: "DraftLinesJson",
                table: "GoodReceiptDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsProduceBatches_ProductionOrderId",
                table: "GoodsProduceBatches",
                column: "ProductionOrderId",
                unique: true);
        }
    }
}
