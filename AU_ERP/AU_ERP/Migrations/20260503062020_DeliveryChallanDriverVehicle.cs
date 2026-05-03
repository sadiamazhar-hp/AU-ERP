using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryChallanDriverVehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "DeliveryChallans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "DeliveryChallans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_DriverId",
                table: "DeliveryChallans",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_VehicleId",
                table: "DeliveryChallans",
                column: "VehicleId");

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
                name: "IX_DeliveryChallans_DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryChallans_VehicleId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "DeliveryChallans");
        }
    }
}
