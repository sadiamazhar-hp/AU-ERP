using System;
using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260516195700_AddDeliveryChallanDeliveryCompleted")]
    public partial class AddDeliveryChallanDeliveryCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryCompletedAt",
                table: "DeliveryChallans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCompletedByUserId",
                table: "DeliveryChallans",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCompletedAt",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "DeliveryCompletedByUserId",
                table: "DeliveryChallans");
        }
    }
}
