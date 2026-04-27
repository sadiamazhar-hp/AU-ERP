using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class PlantScopedInventoryStockAndUserDept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines");

            migrationBuilder.AddColumn<string>(
                name: "PlantID",
                table: "ApplicationUserDepartments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantID",
                table: "StockInventoryLines",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql(
                """
                DECLARE @def nvarchar(450) = N'Man102';

                UPDATE dbo.StockInventoryLines SET PlantID = @def WHERE PlantID IS NULL;

                UPDATE aud SET PlantID = @def
                FROM dbo.ApplicationUserDepartments AS aud
                INNER JOIN dbo.Departments AS d ON d.Id = aud.DepartmentId
                WHERE d.Code = N'Store' AND aud.PlantID IS NULL;

                ;WITH grp AS (
                    SELECT
                        PlantID,
                        MaterialNumber,
                        QuantityUomId,
                        Status,
                        Grade,
                        MIN(Id) AS KeepId,
                        SUM(Quantity) AS SumQ
                    FROM dbo.StockInventoryLines
                    GROUP BY PlantID, MaterialNumber, QuantityUomId, Status, Grade
                    HAVING COUNT(*) > 1
                )
                UPDATE sil SET
                    Quantity = g.SumQ,
                    StockValue = ROUND(g.SumQ * sil.StandardCostPerUom, 2, 1),
                    UpdatedAt = SYSUTCDATETIME()
                FROM dbo.StockInventoryLines sil
                INNER JOIN grp g ON sil.Id = g.KeepId;

                DELETE sil
                FROM dbo.StockInventoryLines sil
                INNER JOIN (
                    SELECT
                        PlantID,
                        MaterialNumber,
                        QuantityUomId,
                        Status,
                        Grade,
                        MIN(Id) AS KeepId,
                        SUM(Quantity) AS SumQ
                    FROM dbo.StockInventoryLines
                    GROUP BY PlantID, MaterialNumber, QuantityUomId, Status, Grade
                    HAVING COUNT(*) > 1
                ) g ON sil.PlantID = g.PlantID
                    AND sil.MaterialNumber = g.MaterialNumber
                    AND sil.QuantityUomId = g.QuantityUomId
                    AND sil.Status = g.Status
                    AND sil.Grade = g.Grade
                    AND sil.Id <> g.KeepId;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PlantID",
                table: "StockInventoryLines",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserDepartments_PlantID",
                table: "ApplicationUserDepartments",
                column: "PlantID");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_PlantID",
                table: "StockInventoryLines",
                column: "PlantID");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines",
                columns: ["PlantID", "MaterialNumber", "QuantityUomId", "Status", "Grade"],
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserDepartments_PlantsSamples_PlantID",
                table: "ApplicationUserDepartments",
                column: "PlantID",
                principalTable: "PlantsSamples",
                principalColumn: "PlantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockInventoryLines_PlantsSamples_PlantID",
                table: "StockInventoryLines",
                column: "PlantID",
                principalTable: "PlantsSamples",
                principalColumn: "PlantID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserDepartments_PlantsSamples_PlantID",
                table: "ApplicationUserDepartments");

            migrationBuilder.DropForeignKey(
                name: "FK_StockInventoryLines_PlantsSamples_PlantID",
                table: "StockInventoryLines");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUserDepartments_PlantID",
                table: "ApplicationUserDepartments");

            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_PlantID_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines");

            migrationBuilder.DropIndex(
                name: "IX_StockInventoryLines_PlantID",
                table: "StockInventoryLines");

            migrationBuilder.DropColumn(
                name: "PlantID",
                table: "ApplicationUserDepartments");

            migrationBuilder.DropColumn(
                name: "PlantID",
                table: "StockInventoryLines");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines",
                columns: ["MaterialNumber", "QuantityUomId", "Status", "Grade"]);
        }
    }
}
