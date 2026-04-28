using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<ApplicationUserDepartment> ApplicationUserDepartments { get; set; }

        public DbSet<CreateMaterialMaster> CreateMaterialMaster { get; set; }
        public DbSet<MaterialNumberRange> MaterialNumberRanges { get; set; }
        public DbSet<UnitConversion> UnitConversions { get; set; }
        public DbSet<MaterialType> MaterialTypes { get; set; }
        public DbSet<MaterialGroup> MaterialGroups { get; set; }
        public DbSet<BOMLevelsSample> BOMLevelsSamples { get; set; }
        public DbSet<BomHeadersSample> BomHeadersSamples { get; set; }
        public DbSet<BomItemsSample> BomItemsSamples { get; set; }
        public DbSet<BusinessPartnerMasterSample> BusinessPartnerMasterSamples { get; set; }
        public DbSet<BPRole> BPRoles { get; set; }
        public DbSet<DistributionChannel> DistributionChannels { get; set; }
        public DbSet<SalesSchemaRow> SalesSchemaRows { get; set; }
        public DbSet<PurchaseSchemeRow> PurchaseSchemeRows { get; set; }
        public DbSet<BPTypeSample> BPTypeSamples { get; set; }
        public DbSet<BPGrouping> BPGroupings { get; set; }
        public DbSet<BPTypeNumberRanges> BPTypeNumberRanges { get; set; }
        public DbSet<PlantsSample> PlantsSamples { get; set; }
        public DbSet<WorkCenterMasterSample> WorkCenterMasterSamples { get; set; }
        public DbSet<RoutingHeadersSample> RoutingHeadersSamples { get; set; }
        public DbSet<RoutingOperationHeaderSample> RoutingOperationHeadersSamples { get; set; }
        public DbSet<RoutingOperationsSample> RoutingOperationsSamples { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentRange> DocumentRanges { get; set; }
        public DbSet<ProductionVersion> ProductionVersions { get; set; }
        public DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderStageProgress> ProductionOrderStageProgresses { get; set; }
        public DbSet<StockInventoryLine> StockInventoryLines { get; set; }
        public DbSet<GoodsProduceBatch> GoodsProduceBatches { get; set; }
        public DbSet<Charge> Charges { get; set; }
        public DbSet<SalesQuotation> SalesQuotations { get; set; }
        public DbSet<SalesQuotationItem> SalesQuotationItems { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<ConfigurationSchema> ConfigurationSchemas { get; set; }
        public DbSet<ConfigurationSchemaCharge> ConfigurationSchemaCharges { get; set; }
        public DbSet<GlobalUnitConversion> GlobalUnitConversions { get; set; }
        public DbSet<DeliveryChallan> DeliveryChallans { get; set; }
        public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            modelBuilder.Entity<ApplicationUserDepartment>()
                .HasKey(ud => new { ud.UserId, ud.DepartmentId });

            modelBuilder.Entity<ApplicationUserDepartment>()
                .HasOne(ud => ud.User)
                .WithMany(u => u.UserDepartments)
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUserDepartment>()
                .HasOne(ud => ud.Department)
                .WithMany(d => d.UserDepartments)
                .HasForeignKey(ud => ud.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationUserDepartment>()
                .Property(ud => ud.PlantID)
                .HasMaxLength(450);

            modelBuilder.Entity<ApplicationUserDepartment>()
                .HasOne(ud => ud.Plant)
                .WithMany()
                .HasForeignKey(ud => ud.PlantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Code = "Store", Name = "Store" },
                new Department { Id = 2, Code = "Sales", Name = "Sales" },
                new Department { Id = 3, Code = "Production", Name = "Production" },
                new Department { Id = 4, Code = "Admin", Name = "Admin" },
                new Department { Id = 5, Code = "Finance", Name = "Finance" });

            modelBuilder.Entity<CreateMaterialMaster>()
                .HasOne(m => m.MaterialType)
                .WithMany(t => t.CreateMaterialMasters)
                .HasForeignKey(m => m.MaterialTypeCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreateMaterialMaster>()
                .HasOne(m => m.MaterialGroup)
                .WithMany(g => g.CreateMaterialMasters)
                .HasForeignKey(m => m.MaterialGroupCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreateMaterialMaster>()
                .Property(m => m.SalesPriceGradeAPerBaseUom)
                .HasPrecision(18, 4);
            modelBuilder.Entity<CreateMaterialMaster>()
                .Property(m => m.SalesPriceGradeBPerBaseUom)
                .HasPrecision(18, 4);
            modelBuilder.Entity<CreateMaterialMaster>()
                .Property(m => m.SalesPriceGradeCPerBaseUom)
                .HasPrecision(18, 4);
            modelBuilder.Entity<CreateMaterialMaster>()
                .Property(m => m.ScrapCostPerBaseUom)
                .HasPrecision(18, 4);

            modelBuilder.Entity<MaterialNumberRange>()
                .HasOne(r => r.MaterialType)
                .WithMany(t => t.MaterialNumberRanges)
                .HasForeignKey(r => r.MaterialTypeCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaterialType>().HasData(
                new MaterialType { MaterialTypeCode = "FERT", Description = "Finished products" },
                new MaterialType { MaterialTypeCode = "HALB", Description = "Semifinished products" },
                new MaterialType { MaterialTypeCode = "PACK", Description = "Packaging" },
                new MaterialType { MaterialTypeCode = "ROH", Description = "Raw materials" }
            );

            modelBuilder.Entity<MaterialNumberRange>()
                .Property(r => r.RangeID)
                .UseIdentityColumn();

            modelBuilder.Entity<UnitConversion>()
                .HasOne(u => u.AltUnit)
                .WithMany()
                .HasForeignKey(u => u.AltUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitConversion>()
                .HasOne(u => u.GlobalUnitConversion)
                .WithMany()
                .HasForeignKey(u => u.GlobalUnitConversionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitConversion>()
                .HasIndex(u => new { u.MaterialNumber, u.AltUnitId })
                .IsUnique();

            modelBuilder.Entity<BomItemsSample>()
                .Property(e => e.ItemID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BomHeadersSample>()
                .Property(h => h.BaseQty)
                .HasPrecision(18, 6);

            modelBuilder.Entity<BomItemsSample>()
                .Property(i => i.Quantity)
                .HasPrecision(18, 6);

            modelBuilder.Entity<BomItemsSample>()
                .Property(i => i.ScrapPercentage)
                .HasPrecision(9, 4);

            modelBuilder.Entity<BomItemsSample>()
                .HasOne(i => i.BomHeadersSample)
                .WithMany(h => h.BomItemsSamples)
                .HasForeignKey(i => i.BomID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BomItemsSample>()
                .HasOne(i => i.CreateMaterialMaster)
                .WithMany(m => m.BomItemsSamples)
                .HasForeignKey(i => i.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomItemsSample>()
                .HasOne(i => i.Uom)
                .WithMany()
                .HasForeignKey(i => i.UomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.BOMLevel)
                .WithMany(l => l.BomHeadersSamples)
                .HasForeignKey(h => h.BLevel)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .Property(h => h.BOMCode)
                .HasMaxLength(20);

            // modelBuilder.Entity<BomHeadersSample>()
            //     .HasOne(h => h.MaterialType)
            //     .WithMany(t => t.BomHeadersSamples)
            //     .HasForeignKey(h => h.MaterialTypeCode)
            //     .OnDelete(DeleteBehavior.Restrict);

            // modelBuilder.Entity<BomHeadersSample>()
            //     .HasOne(h => h.MaterialMaster)
            //     .WithMany()
            //     .HasForeignKey(h => h.MaterialNumber)
            //     .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.PlantSample)
                .WithMany()
                .HasForeignKey(h => h.Plant)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.AlternativeBom)
                .WithMany(h => h.DependentBomsWithThisAlternative)
                .HasForeignKey(h => h.AlternativeBOM)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BPRole>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPRole>()
                .HasIndex(e => e.RoleCode)
                .IsUnique();

            // FLCU00 (Basic): both Sales & Purchasing tabs; FLCU01: Sales; FLVN01: Purchasing.
            modelBuilder.Entity<BPRole>().HasData(
                new BPRole { Id = 1, RoleCode = "FLCU00", RoleName = "Basic" },
                new BPRole { Id = 2, RoleCode = "FLCU01", RoleName = "Customer (Sales)" },
                new BPRole { Id = 3, RoleCode = "FLVN01", RoleName = "Vendor" });

            modelBuilder.Entity<BPTypeSample>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPTypeSample>().HasData(
                new BPTypeSample { Id = 1, TypeName = "Customer", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BPTypeSample { Id = 2, TypeName = "Vendor", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<BPGrouping>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPGrouping>().HasData(
                new BPGrouping { Id = 1, GroupName = "local" }
            );

            modelBuilder.Entity<DistributionChannel>()
                .Property(e => e.DistributionChannelID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DistributionChannel>().HasData(
                new DistributionChannel { DistributionChannelID = 1, DistributionChannelName = "OnCall" },
                new DistributionChannel { DistributionChannelID = 2, DistributionChannelName = "Direct Sales" }
            );

            modelBuilder.Entity<SalesSchemaRow>()
                .Property(e => e.ConditionTypeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<PurchaseSchemeRow>()
                .Property(e => e.ConditionID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.Role)
                .WithMany(r => r.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.TypeSample)
                .WithMany(t => t.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.Grouping)
                .WithMany(g => g.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPGroupingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BPTypeNumberRanges>()
                .Property(e => e.RangeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPTypeNumberRanges>()
                .HasOne(r => r.TypeSample)
                .WithMany(t => t.BPTypeNumberRanges)
                .HasForeignKey(r => r.BPTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkCenterMasterSample>()
                .Property(e => e.ID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<WorkCenterMasterSample>()
                .Property(w => w.SetupTime)
                .HasPrecision(18, 6);

            modelBuilder.Entity<WorkCenterMasterSample>()
                .Property(w => w.MachineTime)
                .HasPrecision(18, 6);

            modelBuilder.Entity<WorkCenterMasterSample>()
                .Property(w => w.LaborTime)
                .HasPrecision(18, 6);

            modelBuilder.Entity<WorkCenterMasterSample>()
                .HasOne(w => w.Plant)
                .WithMany(p => p.WorkCenters)
                .HasForeignKey(w => w.PlantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoutingHeadersSample>()
                .Property(e => e.RoutingID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RoutingHeadersSample>()
                .HasOne(h => h.Plant)
                .WithMany(p => p.RoutingHeaders)
                .HasForeignKey(h => h.PlantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoutingHeadersSample>()
                .HasOne(h => h.Material)
                .WithMany(m => m.RoutingHeadersSamples)
                .HasForeignKey(h => h.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoutingOperationHeaderSample>()
                .Property(e => e.OperationHeaderId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RoutingOperationHeaderSample>()
                .HasOne(h => h.RoutingHeader)
                .WithMany(r => r.OperationHeaders)
                .HasForeignKey(h => h.RoutingID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoutingOperationsSample>()
                .Property(e => e.OpID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RoutingOperationsSample>()
                .Property(o => o.MachineTime)
                .HasPrecision(18, 6);

            modelBuilder.Entity<RoutingOperationsSample>()
                .Property(o => o.LaborTime)
                .HasPrecision(18, 6);

            modelBuilder.Entity<RoutingOperationsSample>()
                .HasOne(o => o.OperationHeader)
                .WithMany(h => h.RoutingOperationsSamples)
                .HasForeignKey(o => o.OperationHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoutingOperationsSample>()
                .HasOne(o => o.WorkCenter)
                .WithMany(w => w.RoutingOperations)
                .HasForeignKey(o => o.WorkCenterID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentType>()
                .Property(e => e.DocumentTypeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DocumentRange>()
                .Property(e => e.RangeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DocumentRange>()
                .HasOne(d => d.DocumentType)
                .WithMany(t => t.DocumentRanges)
                .HasForeignKey(d => d.DocumentTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnitOfMeasurement>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ProductionVersion>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ProductionVersion>()
                .HasOne(p => p.Plant)
                .WithMany()
                .HasForeignKey(p => p.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionVersion>()
                .HasOne(p => p.BomHeader)
                .WithMany()
                .HasForeignKey(p => p.BomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionVersion>()
                .HasOne(p => p.RoutingHeader)
                .WithMany()
                .HasForeignKey(p => p.RoutingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ProductionOrder>()
                .HasIndex(p => p.ProductionNumber)
                .IsUnique();

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.FinishedMaterial)
                .WithMany()
                .HasForeignKey(p => p.FinishedMaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.Uom)
                .WithMany()
                .HasForeignKey(p => p.UomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.ReleasedRouting)
                .WithMany()
                .HasForeignKey(p => p.ReleasedRoutingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(s => s.PlannedHours)
                .HasPrecision(18, 6);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(s => s.ActualHours)
                .HasPrecision(18, 6);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(s => s.InputQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(s => s.OutputQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .Property(s => s.WastageQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .HasOne(s => s.ProductionOrder)
                .WithMany(p => p.StageProgresses)
                .HasForeignKey(s => s.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionOrderStageProgress>()
                .HasOne(s => s.RoutingOperationHeader)
                .WithMany()
                .HasForeignKey(s => s.RoutingOperationHeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(g => g.ProducedQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(g => g.QtyFirstQuality)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(g => g.QtySecondQuality)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(g => g.QtyThirdQuality)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsProduceBatch>()
                .Property(g => g.RejectedScrapQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsProduceBatch>()
                .HasIndex(g => g.ProductionOrderId)
                .IsUnique();

            modelBuilder.Entity<GoodsProduceBatch>()
                .HasOne(g => g.ProductionOrder)
                .WithMany(p => p.GoodsProduceBatches)
                .HasForeignKey(g => g.ProductionOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInventoryLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.Quantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.StandardCostPerUom)
                .HasPrecision(18, 4);

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.StockValue)
                .HasPrecision(18, 2);

            modelBuilder.Entity<StockInventoryLine>()
                .HasIndex(s => s.MaterialNumber);

            modelBuilder.Entity<StockInventoryLine>()
                .HasIndex(s => s.QuantityUomId);

            modelBuilder.Entity<StockInventoryLine>()
                .HasIndex(s => s.Status);

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.PlantID)
                .HasMaxLength(450);

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.Grade)
                .HasMaxLength(32)
                .HasDefaultValue("");

            modelBuilder.Entity<StockInventoryLine>()
                .Property(s => s.BatchOrLot)
                .HasMaxLength(64);

            modelBuilder.Entity<StockInventoryLine>()
                .HasIndex(s => new { s.PlantID, s.MaterialNumber, s.QuantityUomId, s.Status, s.Grade })
                .IsUnique();

            modelBuilder.Entity<StockInventoryLine>()
                .HasOne(s => s.Material)
                .WithMany()
                .HasForeignKey(s => s.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInventoryLine>()
                .HasOne(s => s.QuantityUom)
                .WithMany()
                .HasForeignKey(s => s.QuantityUomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInventoryLine>()
                .HasOne(s => s.Plant)
                .WithMany()
                .HasForeignKey(s => s.PlantID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BOMLevelsSample>().HasData(
                new BOMLevelsSample { LevelID = 1, LevelName = "FG" },
                new BOMLevelsSample { LevelID = 2, LevelName = "SFG" }
            );

            modelBuilder.Entity<Charge>()
                .Property(c => c.Symbol)
                .HasMaxLength(32);

            modelBuilder.Entity<Charge>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Charge>()
                .Property(c => c.ValueType)
                .HasMaxLength(20);

            modelBuilder.Entity<Charge>()
                .Property(c => c.Sign)
                .HasMaxLength(20);

            modelBuilder.Entity<Charge>()
                .HasIndex(c => c.Symbol)
                .IsUnique();

            modelBuilder.Entity<Charge>()
                .Property(c => c.DefaultPercent)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotation>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesQuotation>()
                .Property(s => s.QuotationNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesQuotation>()
                .Property(s => s.PlantId)
                .HasMaxLength(450);

            modelBuilder.Entity<SalesQuotation>()
                .Property(s => s.Status)
                .HasMaxLength(20);

            modelBuilder.Entity<SalesQuotation>()
                .HasIndex(s => s.QuotationNumber)
                .IsUnique();

            modelBuilder.Entity<SalesQuotation>()
                .HasOne(s => s.Plant)
                .WithMany()
                .HasForeignKey(s => s.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesQuotation>()
                .HasOne(s => s.DistributionChannel)
                .WithMany()
                .HasForeignKey(s => s.DistributionChannelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesQuotation>()
                .HasOne(s => s.ConfigurationSchema)
                .WithMany()
                .HasForeignKey(s => s.ConfigurationSchemaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesQuotation>()
                .HasOne(s => s.CustomerBusinessPartner)
                .WithMany()
                .HasForeignKey(s => s.CustomerBusinessPartnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.MaterialNumber)
                .HasMaxLength(32);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.OrderQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.NetPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.DiscountPercent)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.SubtotalAfterDiscount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .Property(i => i.TaxAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesQuotationItem>()
                .HasOne(i => i.SalesQuotation)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SalesQuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesQuotationItem>()
                .HasOne(i => i.QuantityUom)
                .WithMany()
                .HasForeignKey(i => i.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesQuotationItem>()
                .HasOne(i => i.LineTaxCharge)
                .WithMany()
                .HasForeignKey(i => i.LineTaxChargeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrder>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.SalesOrderNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.PlantId)
                .HasMaxLength(450);

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.Status)
                .HasMaxLength(20);

            modelBuilder.Entity<SalesOrder>()
                .HasIndex(s => s.SalesOrderNumber)
                .IsUnique();

            modelBuilder.Entity<SalesOrder>()
                .HasIndex(s => s.SalesQuotationId);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.SalesQuotation)
                .WithMany()
                .HasForeignKey(s => s.SalesQuotationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.Plant)
                .WithMany()
                .HasForeignKey(s => s.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.DistributionChannel)
                .WithMany()
                .HasForeignKey(s => s.DistributionChannelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.ConfigurationSchema)
                .WithMany()
                .HasForeignKey(s => s.ConfigurationSchemaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.CustomerBusinessPartner)
                .WithMany()
                .HasForeignKey(s => s.CustomerBusinessPartnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.MaterialNumber)
                .HasMaxLength(32);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.OrderQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.NetPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.DiscountPercent)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.SubtotalAfterDiscount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .Property(i => i.TaxAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.SalesOrder)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.QuantityUom)
                .WithMany()
                .HasForeignKey(i => i.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.LineTaxCharge)
                .WithMany()
                .HasForeignKey(i => i.LineTaxChargeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ConfigurationSchema>()
                .Property(s => s.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<ConfigurationSchema>()
                .HasIndex(s => s.Title)
                .IsUnique();

            modelBuilder.Entity<ConfigurationSchemaCharge>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ConfigurationSchemaCharge>()
                .HasIndex(x => new { x.ConfigurationSchemaId, x.ChargeId })
                .IsUnique();

            modelBuilder.Entity<ConfigurationSchemaCharge>()
                .HasOne(x => x.ConfigurationSchema)
                .WithMany(s => s.SchemaCharges)
                .HasForeignKey(x => x.ConfigurationSchemaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConfigurationSchemaCharge>()
                .HasOne(x => x.Charge)
                .WithMany()
                .HasForeignKey(x => x.ChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GlobalUnitConversion>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 6);

            modelBuilder.Entity<GlobalUnitConversion>()
                .Property(x => x.Title)
                .HasMaxLength(100);

            modelBuilder.Entity<GlobalUnitConversion>()
                .HasIndex(x => x.Title)
                .IsUnique();

            modelBuilder.Entity<GlobalUnitConversion>()
                .HasIndex(x => new { x.BaseUnitId, x.AltUnitId });

            modelBuilder.Entity<GlobalUnitConversion>()
                .HasOne(x => x.BaseUnit)
                .WithMany()
                .HasForeignKey(x => x.BaseUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GlobalUnitConversion>()
                .HasOne(x => x.AltUnit)
                .WithMany()
                .HasForeignKey(x => x.AltUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryChallan>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DeliveryChallan>()
                .Property(d => d.DeliveryChallanNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<DeliveryChallan>()
                .HasIndex(d => d.DeliveryChallanNumber)
                .IsUnique();

            modelBuilder.Entity<DeliveryChallan>()
                .HasIndex(d => d.SalesOrderId);

            modelBuilder.Entity<DeliveryChallan>()
                .HasOne(d => d.Plant)
                .WithMany()
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryChallan>()
                .HasOne(d => d.ShipToBusinessPartner)
                .WithMany()
                .HasForeignKey(d => d.ShipToBusinessPartnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DeliveryChallan>()
                .HasOne(d => d.SalesOrder)
                .WithMany()
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DeliveryChallanItem>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DeliveryChallanItem>()
                .Property(i => i.DeliveryQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<DeliveryChallanItem>()
                .Property(i => i.MaterialNumber)
                .HasMaxLength(32);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasOne(i => i.DeliveryChallan)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DeliveryChallanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasOne(i => i.SalesOrderItem)
                .WithMany()
                .HasForeignKey(i => i.SalesOrderItemId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasIndex(i => i.QuantityUomId);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasOne(i => i.QuantityUom)
                .WithMany()
                .HasForeignKey(i => i.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlantsSample>().HasData(
                new PlantsSample { PlantID = "Emp101", PlantName = "Emporium" },
                new PlantsSample { PlantID = "Man102", PlantName = "Manufacturing Plant" }
             
            );
        }
    }
}
