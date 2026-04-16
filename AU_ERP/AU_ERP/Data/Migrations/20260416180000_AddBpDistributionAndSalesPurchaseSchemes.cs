using AU_ERP.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AU_ERP.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260416180000_AddBpDistributionAndSalesPurchaseSchemes")]
    public class AddBpDistributionAndSalesPurchaseSchemes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Distribution_Channel",
                columns: table => new
                {
                    DistributionChannelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistributionChannelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribution_Channel", x => x.DistributionChannelID);
                });

            migrationBuilder.CreateTable(
                name: "Sales_Schema",
                columns: table => new
                {
                    ConditionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConditionDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalesType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales_Schema", x => x.ConditionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Purchase_Scheme",
                columns: table => new
                {
                    ConditionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConditionSchema = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchase_Scheme", x => x.ConditionID);
                });

            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [Distribution_Channel] ON;
INSERT INTO [Distribution_Channel] ([DistributionChannelID],[DistributionChannelName]) VALUES
(1, N'On Call'),
(2, N'Direct Sales');
SET IDENTITY_INSERT [Distribution_Channel] OFF;

SET IDENTITY_INSERT [Sales_Schema] ON;
INSERT INTO [Sales_Schema] ([ConditionTypeID],[ConditionType],[ConditionDescription],[SalesType]) VALUES
(1, N'PR00', N'Base Price', N'Walk-In'),
(2, N'ZMRP', N'Retail Price', N'Walk-In'),
(3, N'ZDISC', N'Discount', N'Walk-In'),
(4, N'MWST', N'Tax', N'Walk-In'),
(5, N'METP', N'Final Price', N'Walk-In'),
(6, N'PR00', N'Base Price', N'Dealer'),
(7, N'K004', N'Material Discount', N'Dealer'),
(8, N'K005', N'Dealer Discount', N'Dealer'),
(9, N'ZD01', N'Special Dealer Discount', N'Dealer'),
(10, N'RA01', N'Freight', N'Dealer'),
(11, N'MWST', N'Tax (GST / VAT)', N'Dealer'),
(12, N'NETP', N'Net Price', N'Dealer');
SET IDENTITY_INSERT [Sales_Schema] OFF;

SET IDENTITY_INSERT [Purchase_Scheme] ON;
INSERT INTO [Purchase_Scheme] ([ConditionID],[ConditionType],[ConditionSchema]) VALUES
(1, N'PB00', N'Gross Price'),
(2, N'RA01', N'Discount from Supplier'),
(3, N'ZD01', N'Additional Discount'),
(4, N'FRA1', N'Freight Charges'),
(5, N'ZLC1', N'Loading / Unloading'),
(6, N'MWST', N'Tax (GST / VAT)'),
(7, N'NAVS', N'Non-deductible Tax'),
(8, N'NETP', N'Net Price');
SET IDENTITY_INSERT [Purchase_Scheme] OFF;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Purchase_Scheme");
            migrationBuilder.DropTable(name: "Sales_Schema");
            migrationBuilder.DropTable(name: "Distribution_Channel");
        }
    }
}
