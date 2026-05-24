using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDriverBpAddDriversVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('tempdb..#DriverBp') IS NOT NULL DROP TABLE #DriverBp;
SELECT DISTINCT b.BPID INTO #DriverBp FROM [BusinessPartnerMasterSamples] b
INNER JOIN [BPRoles] r ON b.[BPRoleId] = r.[Id] AND r.[RoleCode] = N'FLDR01'
UNION
SELECT DISTINCT b2.[BPID] FROM [BusinessPartnerMasterSamples] b2 WHERE b2.[BPTypeId] = 3;

UPDATE [SalesOrders] SET [CustomerBusinessPartnerId] = NULL WHERE [CustomerBusinessPartnerId] IN (SELECT BPID FROM #DriverBp);
UPDATE [SalesQuotations] SET [CustomerBusinessPartnerId] = NULL WHERE [CustomerBusinessPartnerId] IN (SELECT BPID FROM #DriverBp);
UPDATE [DeliveryChallans] SET [ShipToBusinessPartnerId] = NULL WHERE [ShipToBusinessPartnerId] IN (SELECT BPID FROM #DriverBp);
UPDATE [SalesInvoices] SET [DealerBusinessPartnerId] = NULL WHERE [DealerBusinessPartnerId] IN (SELECT BPID FROM #DriverBp);

DELETE FROM [BusinessPartnerMasterSamples] WHERE [BPID] IN (SELECT BPID FROM #DriverBp);
DELETE FROM [BPTypeNumberRanges] WHERE [BPTypeId] = 3;
DELETE FROM [BPTypeSamples] WHERE [Id] = 3;
DELETE FROM [BPRoles] WHERE [Id] = 4;
");

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CNIC = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LicenceNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumberPlate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CarType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NumberPlate",
                table: "Vehicles",
                column: "NumberPlate",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallans_Drivers_DriverId",
                table: "DeliveryChallans",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryChallans_Vehicles_VehicleId",
                table: "DeliveryChallans",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallans_Drivers_DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryChallans_Vehicles_VehicleId",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_NumberPlate",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.InsertData(
                table: "BPRoles",
                columns: new[] { "Id", "RoleCode", "RoleName" },
                values: new object[] { 4, "FLDR01", "Driver" });

            migrationBuilder.InsertData(
                table: "BPTypeSamples",
                columns: new[] { "Id", "CreatedAt", "IsActive", "TypeName" },
                values: new object[] { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Driver" });
        }
    }
}
