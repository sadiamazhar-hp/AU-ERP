using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417000000_RoutingOperationHeaders")]
    public partial class RoutingOperationHeaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoutingOperationHeadersSamples",
                columns: table => new
                {
                    OperationHeaderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoutingID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperationHeadersSamples", x => x.OperationHeaderId);
                    table.ForeignKey(
                        name: "FK_RoutingOperationHeadersSamples_RoutingHeadersSamples_RoutingID",
                        column: x => x.RoutingID,
                        principalTable: "RoutingHeadersSamples",
                        principalColumn: "RoutingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationHeadersSamples_RoutingID",
                table: "RoutingOperationHeadersSamples",
                column: "RoutingID");

            migrationBuilder.AddColumn<int>(
                name: "OperationHeaderId",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                DELETE FROM [RoutingOperationsSamples] WHERE [RoutingID] IS NULL;

                DECLARE @OpID INT, @RoutingID INT, @OpSeq INT;
                DECLARE @Desc NVARCHAR(MAX);
                DECLARE @NewHid INT;
                DECLARE @Title NVARCHAR(500);

                DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
                  SELECT [OpID], [RoutingID], [OperationSequence], [Description]
                  FROM [RoutingOperationsSamples]
                  WHERE [RoutingID] IS NOT NULL
                  ORDER BY [OpID];

                OPEN cur;
                FETCH NEXT FROM cur INTO @OpID, @RoutingID, @OpSeq, @Desc;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                  SET @Title = CASE
                    WHEN @Desc IS NOT NULL AND LEN(LTRIM(RTRIM(@Desc))) > 0 THEN LEFT(LTRIM(RTRIM(@Desc)), 500)
                    ELSE N'Operation ' + CAST(@OpSeq AS NVARCHAR(20))
                  END;
                  INSERT INTO [RoutingOperationHeadersSamples] ([RoutingID], [Title], [DisplayOrder])
                  VALUES (@RoutingID, @Title, @OpSeq);
                  SET @NewHid = SCOPE_IDENTITY();
                  UPDATE [RoutingOperationsSamples] SET [OperationHeaderId] = @NewHid WHERE [OpID] = @OpID;
                  FETCH NEXT FROM cur INTO @OpID, @RoutingID, @OpSeq, @Desc;
                END

                CLOSE cur;
                DEALLOCATE cur;
                ");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutingOperationsSamples_RoutingHeadersSamples_RoutingID",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropIndex(
                name: "IX_RoutingOperationsSamples_RoutingID",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropColumn(
                name: "RoutingID",
                table: "RoutingOperationsSamples");

            migrationBuilder.AlterColumn<int>(
                name: "OperationHeaderId",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_OperationHeaderId",
                table: "RoutingOperationsSamples",
                column: "OperationHeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutingOperationsSamples_RoutingOperationHeadersSamples_OperationHeaderId",
                table: "RoutingOperationsSamples",
                column: "OperationHeaderId",
                principalTable: "RoutingOperationHeadersSamples",
                principalColumn: "OperationHeaderId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoutingOperationsSamples_RoutingOperationHeadersSamples_OperationHeaderId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropIndex(
                name: "IX_RoutingOperationsSamples_OperationHeaderId",
                table: "RoutingOperationsSamples");

            migrationBuilder.AddColumn<int>(
                name: "RoutingID",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ro
                SET ro.[RoutingID] = oh.[RoutingID]
                FROM [RoutingOperationsSamples] AS ro
                INNER JOIN [RoutingOperationHeadersSamples] AS oh ON ro.[OperationHeaderId] = oh.[OperationHeaderId];
                ");

            migrationBuilder.AlterColumn<int>(
                name: "RoutingID",
                table: "RoutingOperationsSamples",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "OperationHeaderId",
                table: "RoutingOperationsSamples");

            migrationBuilder.DropTable(
                name: "RoutingOperationHeadersSamples");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_RoutingID",
                table: "RoutingOperationsSamples",
                column: "RoutingID");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutingOperationsSamples_RoutingHeadersSamples_RoutingID",
                table: "RoutingOperationsSamples",
                column: "RoutingID",
                principalTable: "RoutingHeadersSamples",
                principalColumn: "RoutingID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
