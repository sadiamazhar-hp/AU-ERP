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

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
