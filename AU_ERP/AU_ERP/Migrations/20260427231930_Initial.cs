using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AU_ERP.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordSetupCompleted = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BOMLevelsSamples",
                columns: table => new
                {
                    LevelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMLevelsSamples", x => x.LevelID);
                });

            migrationBuilder.CreateTable(
                name: "BPGroupings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPGroupings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BPRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BPTypeSamples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPTypeSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sign = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationSchemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchemaType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

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
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeID);
                });

            migrationBuilder.CreateTable(
                name: "MaterialGroups",
                columns: table => new
                {
                    MaterialGroupCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorizationGroup = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialGroups", x => x.MaterialGroupCode);
                });

            migrationBuilder.CreateTable(
                name: "MaterialTypes",
                columns: table => new
                {
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialTypes", x => x.MaterialTypeCode);
                });

            migrationBuilder.CreateTable(
                name: "PlantsSamples",
                columns: table => new
                {
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlantName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantsSamples", x => x.PlantID);
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
                name: "UnitOfMeasurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasurements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BPTypeNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BPTypeId = table.Column<int>(type: "int", nullable: true),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartNumber = table.Column<int>(type: "int", nullable: false),
                    EndNumber = table.Column<int>(type: "int", nullable: false),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPTypeNumberRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_BPTypeNumberRanges_BPTypeSamples_BPTypeId",
                        column: x => x.BPTypeId,
                        principalTable: "BPTypeSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPartnerMasterSamples",
                columns: table => new
                {
                    BPID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BPRoleId = table.Column<int>(type: "int", nullable: true),
                    BPTypeId = table.Column<int>(type: "int", nullable: true),
                    BPGroupingId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HouseNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReconAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DistChannel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPartnerMasterSamples", x => x.BPID);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPGroupings_BPGroupingId",
                        column: x => x.BPGroupingId,
                        principalTable: "BPGroupings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPRoles_BPRoleId",
                        column: x => x.BPRoleId,
                        principalTable: "BPRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessPartnerMasterSamples_BPTypeSamples_BPTypeId",
                        column: x => x.BPTypeId,
                        principalTable: "BPTypeSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationSchemaCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationSchemaId = table.Column<int>(type: "int", nullable: false),
                    ChargeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSchemaCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationSchemaCharges_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationSchemaCharges_ConfigurationSchemas_ConfigurationSchemaId",
                        column: x => x.ConfigurationSchemaId,
                        principalTable: "ConfigurationSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserDepartments",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserDepartments", x => new { x.UserId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserDepartments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: true),
                    FromNumber = table.Column<int>(type: "int", nullable: true),
                    ToNumber = table.Column<int>(type: "int", nullable: true),
                    CurrentNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_DocumentRanges_DocumentTypes_DocumentTypeID",
                        column: x => x.DocumentTypeID,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreateMaterialMaster",
                columns: table => new
                {
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IndustrySectorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseUnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialGroupCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Division = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveringPlantCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemCategoryGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesPriceGradeAPerBaseUom = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SalesPriceGradeBPerBaseUom = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SalesPriceGradeCPerBaseUom = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ScrapCostPerBaseUom = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PurchasingGroupCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GrProcessingTime = table.Column<int>(type: "int", nullable: true),
                    Gr_Processing_UOM = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MrpTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcurementTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategyGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    SafetyStock = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReorderPoint = table.Column<int>(type: "int", nullable: true),
                    ValuationClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreateMaterialMaster", x => x.MaterialNumber);
                    table.ForeignKey(
                        name: "FK_CreateMaterialMaster_MaterialGroups_MaterialGroupCode",
                        column: x => x.MaterialGroupCode,
                        principalTable: "MaterialGroups",
                        principalColumn: "MaterialGroupCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreateMaterialMaster_MaterialTypes_MaterialTypeCode",
                        column: x => x.MaterialTypeCode,
                        principalTable: "MaterialTypes",
                        principalColumn: "MaterialTypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialNumberRanges",
                columns: table => new
                {
                    RangeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FromNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsExternal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialNumberRanges", x => x.RangeID);
                    table.ForeignKey(
                        name: "FK_MaterialNumberRanges_MaterialTypes_MaterialTypeCode",
                        column: x => x.MaterialTypeCode,
                        principalTable: "MaterialTypes",
                        principalColumn: "MaterialTypeCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomHeadersSamples",
                columns: table => new
                {
                    BomID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BOMCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BOMTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderMaterialTypeCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BomMaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BLevel = table.Column<int>(type: "int", nullable: true),
                    AlternativeBOM = table.Column<int>(type: "int", nullable: true),
                    Plant = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaseQty = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomHeadersSamples", x => x.BomID);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_BOMLevelsSamples_BLevel",
                        column: x => x.BLevel,
                        principalTable: "BOMLevelsSamples",
                        principalColumn: "LevelID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_BomHeadersSamples_AlternativeBOM",
                        column: x => x.AlternativeBOM,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomHeadersSamples_PlantsSamples_Plant",
                        column: x => x.Plant,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCenterMasterSamples",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkCenterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailableCapacity = table.Column<int>(type: "int", nullable: true),
                    UtilizationPercentage = table.Column<int>(type: "int", nullable: true),
                    SetupTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MachineTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LaborTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TimeUom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCenterMasterSamples", x => x.ID);
                    table.ForeignKey(
                        name: "FK_WorkCenterMasterSamples_PlantsSamples_PlantID",
                        column: x => x.PlantID,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GlobalUnitConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseUnitId = table.Column<int>(type: "int", nullable: false),
                    AltUnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalUnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalUnitConversions_UnitOfMeasurements_AltUnitId",
                        column: x => x.AltUnitId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GlobalUnitConversions_UnitOfMeasurements_BaseUnitId",
                        column: x => x.BaseUnitId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuotationNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DistributionChannelId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationSchemaId = table.Column<int>(type: "int", nullable: true),
                    CustomerBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SalesPersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PriceListCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaymentTerm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationLevelChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeColumnIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShipToAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    QuotationDate = table.Column<DateTime>(type: "date", nullable: false),
                    ValidityDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_BusinessPartnerMasterSamples_CustomerBusinessPartnerId",
                        column: x => x.CustomerBusinessPartnerId,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_ConfigurationSchemas_ConfigurationSchemaId",
                        column: x => x.ConfigurationSchemaId,
                        principalTable: "ConfigurationSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Distribution_Channel_DistributionChannelId",
                        column: x => x.DistributionChannelId,
                        principalTable: "Distribution_Channel",
                        principalColumn: "DistributionChannelID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutingHeadersSamples",
                columns: table => new
                {
                    RoutingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlantID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingHeadersSamples", x => x.RoutingID);
                    table.ForeignKey(
                        name: "FK_RoutingHeadersSamples_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutingHeadersSamples_PlantsSamples_PlantID",
                        column: x => x.PlantID,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockInventoryLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUomId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: ""),
                    StandardCostPerUom = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StockValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BatchOrLot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockInventoryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockInventoryLines_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockInventoryLines_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomItemsSamples",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BomID = table.Column<int>(type: "int", nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UomId = table.Column<int>(type: "int", nullable: true),
                    ScrapPercentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomItemsSamples", x => x.ItemID);
                    table.ForeignKey(
                        name: "FK_BomItemsSamples_BomHeadersSamples_BomID",
                        column: x => x.BomID,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BomItemsSamples_CreateMaterialMaster_MaterialNumber",
                        column: x => x.MaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomItemsSamples_UnitOfMeasurements_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AltUnitId = table.Column<int>(type: "int", nullable: false),
                    Numerator = table.Column<float>(type: "real", nullable: false),
                    Denominator = table.Column<float>(type: "real", nullable: false),
                    GlobalUnitConversionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitConversions_GlobalUnitConversions_GlobalUnitConversionId",
                        column: x => x.GlobalUnitConversionId,
                        principalTable: "GlobalUnitConversions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversions_UnitOfMeasurements_AltUnitId",
                        column: x => x.AltUnitId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: true),
                    SalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DistributionChannelId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationSchemaId = table.Column<int>(type: "int", nullable: true),
                    CustomerBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SalesPersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PriceListCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PaymentTerm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationLevelChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeColumnIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    QuotationChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShipToAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "date", nullable: false),
                    RequestedDeliveryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_BusinessPartnerMasterSamples_CustomerBusinessPartnerId",
                        column: x => x.CustomerBusinessPartnerId,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_ConfigurationSchemas_ConfigurationSchemaId",
                        column: x => x.ConfigurationSchemaId,
                        principalTable: "ConfigurationSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Distribution_Channel_DistributionChannelId",
                        column: x => x.DistributionChannelId,
                        principalTable: "Distribution_Channel",
                        principalColumn: "DistributionChannelID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrders_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesQuotationId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SalesPriceGrade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SubtotalAfterDiscount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTaxChargeId = table.Column<int>(type: "int", nullable: true),
                    ItemAppliedChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_Charges_LineTaxChargeId",
                        column: x => x.LineTaxChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_SalesQuotations_SalesQuotationId",
                        column: x => x.SalesQuotationId,
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotationItems_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedMaterialNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TargetQuantity = table.Column<int>(type: "int", nullable: false),
                    UomId = table.Column<int>(type: "int", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    PlannedEndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleasedRoutingId = table.Column<int>(type: "int", nullable: true),
                    ReleasedBomSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_CreateMaterialMaster_FinishedMaterialNumber",
                        column: x => x.FinishedMaterialNumber,
                        principalTable: "CreateMaterialMaster",
                        principalColumn: "MaterialNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_RoutingHeadersSamples_ReleasedRoutingId",
                        column: x => x.ReleasedRoutingId,
                        principalTable: "RoutingHeadersSamples",
                        principalColumn: "RoutingID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_UnitOfMeasurements_UomId",
                        column: x => x.UomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BomId = table.Column<int>(type: "int", nullable: true),
                    RoutingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_BomHeadersSamples_BomId",
                        column: x => x.BomId,
                        principalTable: "BomHeadersSamples",
                        principalColumn: "BomID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionVersions_RoutingHeadersSamples_RoutingId",
                        column: x => x.RoutingId,
                        principalTable: "RoutingHeadersSamples",
                        principalColumn: "RoutingID",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "DeliveryChallans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryChallanNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlantId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DeliveryType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ShipToBusinessPartnerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ShipToDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "date", nullable: false),
                    SalesOrderId = table.Column<int>(type: "int", nullable: true),
                    ReferenceSalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_BusinessPartnerMasterSamples_ShipToBusinessPartnerId",
                        column: x => x.ShipToBusinessPartnerId,
                        principalTable: "BusinessPartnerMasterSamples",
                        principalColumn: "BPID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_PlantsSamples_PlantId",
                        column: x => x.PlantId,
                        principalTable: "PlantsSamples",
                        principalColumn: "PlantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryChallans_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesOrderId = table.Column<int>(type: "int", nullable: false),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SalesPriceGrade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SubtotalAfterDiscount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTaxChargeId = table.Column<int>(type: "int", nullable: true),
                    ItemAppliedChargeIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ItemChargeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_Charges_LineTaxChargeId",
                        column: x => x.LineTaxChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GoodsProduceBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    GrDate = table.Column<DateTime>(type: "date", nullable: false),
                    ProducedQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyFirstQuality = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtySecondQuality = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QtyThirdQuality = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedScrapQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsProduceBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsProduceBatches_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderStageProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    RoutingOperationHeaderId = table.Column<int>(type: "int", nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    StageTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PlannedHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ActualHours = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    InputQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    OutputQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    WastageQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    WastageReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkerOperator = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StageStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderStageProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageProgresses_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOrderStageProgresses_RoutingOperationHeadersSamples_RoutingOperationHeaderId",
                        column: x => x.RoutingOperationHeaderId,
                        principalTable: "RoutingOperationHeadersSamples",
                        principalColumn: "OperationHeaderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutingOperationsSamples",
                columns: table => new
                {
                    OpID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationHeaderId = table.Column<int>(type: "int", nullable: false),
                    WorkCenterID = table.Column<int>(type: "int", nullable: true),
                    OperationSequence = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LaborTime = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TimeUom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingOperationsSamples", x => x.OpID);
                    table.ForeignKey(
                        name: "FK_RoutingOperationsSamples_RoutingOperationHeadersSamples_OperationHeaderId",
                        column: x => x.OperationHeaderId,
                        principalTable: "RoutingOperationHeadersSamples",
                        principalColumn: "OperationHeaderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutingOperationsSamples_WorkCenterMasterSamples_WorkCenterID",
                        column: x => x.WorkCenterID,
                        principalTable: "WorkCenterMasterSamples",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryChallanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryChallanId = table.Column<int>(type: "int", nullable: false),
                    ReferenceSalesOrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaterialNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaterialDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveryQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityUomId = table.Column<int>(type: "int", nullable: true),
                    Batch = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SalesOrderItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryChallanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryChallanItems_DeliveryChallans_DeliveryChallanId",
                        column: x => x.DeliveryChallanId,
                        principalTable: "DeliveryChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryChallanItems_SalesOrderItems_SalesOrderItemId",
                        column: x => x.SalesOrderItemId,
                        principalTable: "SalesOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryChallanItems_UnitOfMeasurements_QuantityUomId",
                        column: x => x.QuantityUomId,
                        principalTable: "UnitOfMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "BOMLevelsSamples",
                columns: new[] { "LevelID", "LevelName" },
                values: new object[,]
                {
                    { 1, "FG" },
                    { 2, "SFG" }
                });

            migrationBuilder.InsertData(
                table: "BPRoles",
                columns: new[] { "Id", "RoleCode", "RoleName" },
                values: new object[,]
                {
                    { 1, "FLCU00", "Basic" },
                    { 2, "FLCU01", "Customer (Sales)" },
                    { 3, "FLVN01", "Vendor" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "Store", "Store" },
                    { 2, "Sales", "Sales" },
                    { 3, "Production", "Production" },
                    { 4, "Admin", "Admin" },
                    { 5, "Finance", "Finance" }
                });

            migrationBuilder.InsertData(
                table: "PlantsSamples",
                columns: new[] { "PlantID", "PlantName" },
                values: new object[,]
                {
                    { "Emp101", "Emporium" },
                    { "Man102", "Manufacturing Plant" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserDepartments_DepartmentId",
                table: "ApplicationUserDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_AlternativeBOM",
                table: "BomHeadersSamples",
                column: "AlternativeBOM");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_BLevel",
                table: "BomHeadersSamples",
                column: "BLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BomHeadersSamples_Plant",
                table: "BomHeadersSamples",
                column: "Plant");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_BomID",
                table: "BomItemsSamples",
                column: "BomID");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_MaterialNumber",
                table: "BomItemsSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_BomItemsSamples_UomId",
                table: "BomItemsSamples",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_BPRoles_RoleCode",
                table: "BPRoles",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BPTypeNumberRanges_BPTypeId",
                table: "BPTypeNumberRanges",
                column: "BPTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPGroupingId",
                table: "BusinessPartnerMasterSamples",
                column: "BPGroupingId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPRoleId",
                table: "BusinessPartnerMasterSamples",
                column: "BPRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartnerMasterSamples_BPTypeId",
                table: "BusinessPartnerMasterSamples",
                column: "BPTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_Symbol",
                table: "Charges",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemaCharges_ChargeId",
                table: "ConfigurationSchemaCharges",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemaCharges_ConfigurationSchemaId_ChargeId",
                table: "ConfigurationSchemaCharges",
                columns: new[] { "ConfigurationSchemaId", "ChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSchemas_Title",
                table: "ConfigurationSchemas",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreateMaterialMaster_MaterialGroupCode",
                table: "CreateMaterialMaster",
                column: "MaterialGroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_CreateMaterialMaster_MaterialTypeCode",
                table: "CreateMaterialMaster",
                column: "MaterialTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_DeliveryChallanId",
                table: "DeliveryChallanItems",
                column: "DeliveryChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_QuantityUomId",
                table: "DeliveryChallanItems",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallanItems_SalesOrderItemId",
                table: "DeliveryChallanItems",
                column: "SalesOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_DeliveryChallanNumber",
                table: "DeliveryChallans",
                column: "DeliveryChallanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_PlantId",
                table: "DeliveryChallans",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_SalesOrderId",
                table: "DeliveryChallans",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryChallans_ShipToBusinessPartnerId",
                table: "DeliveryChallans",
                column: "ShipToBusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRanges_DocumentTypeID",
                table: "DocumentRanges",
                column: "DocumentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUnitConversions_AltUnitId",
                table: "GlobalUnitConversions",
                column: "AltUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUnitConversions_BaseUnitId_AltUnitId",
                table: "GlobalUnitConversions",
                columns: new[] { "BaseUnitId", "AltUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalUnitConversions_Title",
                table: "GlobalUnitConversions",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsProduceBatches_ProductionOrderId",
                table: "GoodsProduceBatches",
                column: "ProductionOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialNumberRanges_MaterialTypeCode",
                table: "MaterialNumberRanges",
                column: "MaterialTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_FinishedMaterialNumber",
                table: "ProductionOrders",
                column: "FinishedMaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductionNumber",
                table: "ProductionOrders",
                column: "ProductionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ReleasedRoutingId",
                table: "ProductionOrders",
                column: "ReleasedRoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_UomId",
                table: "ProductionOrders",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageProgresses_ProductionOrderId",
                table: "ProductionOrderStageProgresses",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderStageProgresses_RoutingOperationHeaderId",
                table: "ProductionOrderStageProgresses",
                column: "RoutingOperationHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_BomId",
                table: "ProductionVersions",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_PlantId",
                table: "ProductionVersions",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionVersions_RoutingId",
                table: "ProductionVersions",
                column: "RoutingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingHeadersSamples_MaterialNumber",
                table: "RoutingHeadersSamples",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingHeadersSamples_PlantID",
                table: "RoutingHeadersSamples",
                column: "PlantID");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationHeadersSamples_RoutingID",
                table: "RoutingOperationHeadersSamples",
                column: "RoutingID");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_OperationHeaderId",
                table: "RoutingOperationsSamples",
                column: "OperationHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingOperationsSamples_WorkCenterID",
                table: "RoutingOperationsSamples",
                column: "WorkCenterID");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_LineTaxChargeId",
                table: "SalesOrderItems",
                column: "LineTaxChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_QuantityUomId",
                table: "SalesOrderItems",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId",
                table: "SalesOrderItems",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ConfigurationSchemaId",
                table: "SalesOrders",
                column: "ConfigurationSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerBusinessPartnerId",
                table: "SalesOrders",
                column: "CustomerBusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_DistributionChannelId",
                table: "SalesOrders",
                column: "DistributionChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PlantId",
                table: "SalesOrders",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesOrderNumber",
                table: "SalesOrders",
                column: "SalesOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SalesQuotationId",
                table: "SalesOrders",
                column: "SalesQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_LineTaxChargeId",
                table: "SalesQuotationItems",
                column: "LineTaxChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_QuantityUomId",
                table: "SalesQuotationItems",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationItems_SalesQuotationId",
                table: "SalesQuotationItems",
                column: "SalesQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_ConfigurationSchemaId",
                table: "SalesQuotations",
                column: "ConfigurationSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CustomerBusinessPartnerId",
                table: "SalesQuotations",
                column: "CustomerBusinessPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_DistributionChannelId",
                table: "SalesQuotations",
                column: "DistributionChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_PlantId",
                table: "SalesQuotations",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_QuotationNumber",
                table: "SalesQuotations",
                column: "QuotationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_MaterialNumber",
                table: "StockInventoryLines",
                column: "MaterialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_MaterialNumber_QuantityUomId_Status_Grade",
                table: "StockInventoryLines",
                columns: new[] { "MaterialNumber", "QuantityUomId", "Status", "Grade" });

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_QuantityUomId",
                table: "StockInventoryLines",
                column: "QuantityUomId");

            migrationBuilder.CreateIndex(
                name: "IX_StockInventoryLines_Status",
                table: "StockInventoryLines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_AltUnitId",
                table: "UnitConversions",
                column: "AltUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_GlobalUnitConversionId",
                table: "UnitConversions",
                column: "GlobalUnitConversionId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_MaterialNumber_AltUnitId",
                table: "UnitConversions",
                columns: new[] { "MaterialNumber", "AltUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenterMasterSamples_PlantID",
                table: "WorkCenterMasterSamples",
                column: "PlantID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserDepartments");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BomItemsSamples");

            migrationBuilder.DropTable(
                name: "BPTypeNumberRanges");

            migrationBuilder.DropTable(
                name: "ConfigurationSchemaCharges");

            migrationBuilder.DropTable(
                name: "DeliveryChallanItems");

            migrationBuilder.DropTable(
                name: "DocumentRanges");

            migrationBuilder.DropTable(
                name: "GoodsProduceBatches");

            migrationBuilder.DropTable(
                name: "MaterialNumberRanges");

            migrationBuilder.DropTable(
                name: "ProductionOrderStageProgresses");

            migrationBuilder.DropTable(
                name: "ProductionVersions");

            migrationBuilder.DropTable(
                name: "Purchase_Scheme");

            migrationBuilder.DropTable(
                name: "RoutingOperationsSamples");

            migrationBuilder.DropTable(
                name: "Sales_Schema");

            migrationBuilder.DropTable(
                name: "SalesQuotationItems");

            migrationBuilder.DropTable(
                name: "StockInventoryLines");

            migrationBuilder.DropTable(
                name: "UnitConversions");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "DeliveryChallans");

            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropTable(
                name: "BomHeadersSamples");

            migrationBuilder.DropTable(
                name: "RoutingOperationHeadersSamples");

            migrationBuilder.DropTable(
                name: "WorkCenterMasterSamples");

            migrationBuilder.DropTable(
                name: "GlobalUnitConversions");

            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "BOMLevelsSamples");

            migrationBuilder.DropTable(
                name: "RoutingHeadersSamples");

            migrationBuilder.DropTable(
                name: "UnitOfMeasurements");

            migrationBuilder.DropTable(
                name: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "CreateMaterialMaster");

            migrationBuilder.DropTable(
                name: "BusinessPartnerMasterSamples");

            migrationBuilder.DropTable(
                name: "ConfigurationSchemas");

            migrationBuilder.DropTable(
                name: "Distribution_Channel");

            migrationBuilder.DropTable(
                name: "PlantsSamples");

            migrationBuilder.DropTable(
                name: "MaterialGroups");

            migrationBuilder.DropTable(
                name: "MaterialTypes");

            migrationBuilder.DropTable(
                name: "BPGroupings");

            migrationBuilder.DropTable(
                name: "BPRoles");

            migrationBuilder.DropTable(
                name: "BPTypeSamples");
        }
    }
}
