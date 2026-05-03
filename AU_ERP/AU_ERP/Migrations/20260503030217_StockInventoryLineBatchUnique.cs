using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class StockInventoryLineBatchUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize NULL batch to empty string so "no batch" is one bucket (matches app upsert keys).
            migrationBuilder.Sql(
                """
                UPDATE [StockInventoryLines]
                SET [BatchOrLot] = N''
                WHERE [BatchOrLot] IS NULL;
                """);

            // Merge duplicate rows that would violate the new (plant, material, UOM, status, grade, batch) key.
            migrationBuilder.Sql(
                """
                UPDATE sil
                SET sil.[Quantity] = agg.[SumQty],
                    sil.[StockValue] = agg.[SumVal],
                    sil.[StandardCostPerUom] = CASE WHEN agg.[SumQty] > 0 THEN agg.[SumVal] / agg.[SumQty] ELSE sil.[StandardCostPerUom] END,
                    sil.[UpdatedAt] = SYSUTCDATETIME()
                FROM [StockInventoryLines] sil
                INNER JOIN (
                    SELECT MIN([Id]) AS [KeeperId], SUM([Quantity]) AS [SumQty], SUM([StockValue]) AS [SumVal]
                    FROM [StockInventoryLines]
                    GROUP BY [PlantID], [MaterialNumber], [QuantityUomId], [Status], [Grade], [BatchOrLot]
                    HAVING COUNT(*) > 1
                ) agg ON sil.[Id] = agg.[KeeperId];
                """);

            migrationBuilder.Sql(
                """
                DELETE sil
                FROM [StockInventoryLines] sil
                INNER JOIN (
                    SELECT [Id],
                        ROW_NUMBER() OVER (
                            PARTITION BY [PlantID], [MaterialNumber], [QuantityUomId], [Status], [Grade], [BatchOrLot]
                            ORDER BY [Id]) AS rn
                    FROM [StockInventoryLines]
                ) x ON sil.[Id] = x.[Id]
                WHERE x.[rn] > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade_BatchOrLot",
                table: "StockInventoryLines",
                columns: new[] { "PlantID", "MaterialNumber", "QuantityUomId", "Status", "Grade", "BatchOrLot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade_BatchOrLot",
                table: "StockInventoryLines");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines",
                columns: new[] { "PlantID", "MaterialNumber", "QuantityUomId", "Status", "Grade" },
                unique: true);
        }
    }
}
