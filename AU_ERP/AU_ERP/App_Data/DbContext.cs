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
        public DbSet<DocumentIntegration> DocumentIntegrations { get; set; }
        public DbSet<ProductionVersion> ProductionVersions { get; set; }
        public DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderLine> ProductionOrderLines { get; set; }
        public DbSet<ProductionOrderStageProgress> ProductionOrderStageProgresses { get; set; }
        public DbSet<StockInventoryLine> StockInventoryLines { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<GoodsProduceBatch> GoodsProduceBatches { get; set; }
        public DbSet<GoodReceiptDocument> GoodReceiptDocuments { get; set; }
        public DbSet<GoodsIssueDocument> GoodsIssueDocuments { get; set; }
        public DbSet<GoodsIssueDocumentLine> GoodsIssueDocumentLines { get; set; }
        public DbSet<Charge> Charges { get; set; }
        public DbSet<SalesQuotation> SalesQuotations { get; set; }
        public DbSet<SalesQuotationItem> SalesQuotationItems { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
        public DbSet<SalesGoodsIssueDocument> SalesGoodsIssueDocuments { get; set; }
        public DbSet<SalesGoodsIssueDocumentLine> SalesGoodsIssueDocumentLines { get; set; }
        public DbSet<ConfigurationSchema> ConfigurationSchemas { get; set; }
        public DbSet<ConfigurationSchemaCharge> ConfigurationSchemaCharges { get; set; }
        public DbSet<GlobalUnitConversion> GlobalUnitConversions { get; set; }
        public DbSet<DeliveryChallan> DeliveryChallans { get; set; }
        public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; }
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesInvoiceLine> SalesInvoiceLines { get; set; }
        public DbSet<SalesPayment> SalesPayments { get; set; }
        public DbSet<SalesReturnOrder> SalesReturnOrders { get; set; }
        public DbSet<SalesReturnOrderLine> SalesReturnOrderLines { get; set; }
        public DbSet<SalesReturnQualityInspection> SalesReturnQualityInspections { get; set; }
        public DbSet<SalesReturnQualityInspectionLine> SalesReturnQualityInspectionLines { get; set; }
        public DbSet<SalesReturnCreditMemo> SalesReturnCreditMemos { get; set; }
        public DbSet<SalesReturnCreditMemoLine> SalesReturnCreditMemoLines { get; set; }
        public DbSet<CompanyInfo> CompanyInfos { get; set; }

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
                new MaterialType { MaterialTypeCode = "FERT", Description = "Finished products", EnabledTabsJson = "[\"Accounting\",\"BasicData\",\"MRP\",\"Sales\"]" },
                new MaterialType { MaterialTypeCode = "HALB", Description = "Semifinished products", EnabledTabsJson = "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]" },
                new MaterialType { MaterialTypeCode = "PACK", Description = "Packaging", EnabledTabsJson = "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]" },
                new MaterialType { MaterialTypeCode = "ROH", Description = "Raw materials", EnabledTabsJson = "[\"Accounting\",\"BasicData\",\"MRP\",\"Purchasing\"]" }
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
            
            modelBuilder.Entity<BomHeadersSample>()
                .Property(h => h.BomUsage)
                .HasMaxLength(30)
                .HasDefaultValue("Production");
            
            modelBuilder.Entity<BomHeadersSample>()
                .Property(h => h.AlternativeNo)
                .HasMaxLength(30)
                .HasDefaultValue("AUTO");
            
            modelBuilder.Entity<BomHeadersSample>()
                .Property(h => h.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            
            modelBuilder.Entity<BomHeadersSample>()
                .HasIndex(h => new { h.BomMaterialNumber, h.Plant, h.BomUsage, h.AlternativeNo })
                .IsUnique();
            
            modelBuilder.Entity<BomHeadersSample>()
                .HasIndex(h => new { h.BomMaterialNumber, h.Plant, h.IsDefaultBom, h.Status });
            
            modelBuilder.Entity<BomHeadersSample>()
                .HasIndex(h => h.BomMaterialNumber)
                .HasFilter("[IsDefaultBom] = 1")
                .IsUnique();

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
                new BPRole { Id = 3, RoleCode = "FLVN01", RoleName = "Vendor" },
                new BPRole { Id = 4, RoleCode = "FLDR01", RoleName = "Driver" });

            modelBuilder.Entity<BPTypeSample>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPTypeSample>().HasData(
                new BPTypeSample { Id = 1, TypeName = "Customer", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BPTypeSample { Id = 2, TypeName = "Vendor", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BPTypeSample { Id = 3, TypeName = "Driver", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .Property(m => m.FirstName)
                .HasMaxLength(120);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .Property(m => m.LastName)
                .HasMaxLength(120);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .Property(m => m.CNIC)
                .HasMaxLength(32);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .Property(m => m.LicenceNo)
                .HasMaxLength(64);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .Property(m => m.IsActive)
                .HasDefaultValue(true);

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

            modelBuilder.Entity<DocumentIntegration>(e =>
            {
                e.Property(p => p.DocumentIntegrationID).ValueGeneratedOnAdd();
                e.HasIndex(p => p.ModuleKey).IsUnique();
                e.HasOne(p => p.DocumentType)
                    .WithMany()
                    .HasForeignKey(p => p.DocumentTypeID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

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
            
            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.SelectedBom)
                .WithMany()
                .HasForeignKey(p => p.SelectedBomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .Property(p => p.ProductionDocumentNumber)
                .HasMaxLength(32);

            modelBuilder.Entity<ProductionOrderLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ProductionOrderLine>()
                .Property(x => x.PlannedQuantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<ProductionOrderLine>()
                .HasIndex(x => new { x.ProductionOrderId, x.LineNo })
                .IsUnique();

            modelBuilder.Entity<ProductionOrderLine>()
                .HasOne(x => x.ProductionOrder)
                .WithMany(p => p.Lines)
                .HasForeignKey(x => x.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionOrderLine>()
                .HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrderLine>()
                .HasOne(x => x.Uom)
                .WithMany()
                .HasForeignKey(x => x.UomId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ProductionOrderLine>()
                .HasOne(x => x.Plant)
                .WithMany()
                .HasForeignKey(x => x.PlantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ProductionOrderLine>()
                .HasOne(x => x.SelectedBom)
                .WithMany()
                .HasForeignKey(x => x.SelectedBomId)
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
                .HasOne(s => s.ProductionOrderLine)
                .WithMany()
                .HasForeignKey(s => s.ProductionOrderLineId)
                .OnDelete(DeleteBehavior.SetNull);

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
                .HasIndex(g => g.ProductionOrderId);

            modelBuilder.Entity<GoodsProduceBatch>()
                .HasOne(g => g.ProductionOrder)
                .WithMany(p => p.GoodsProduceBatches)
                .HasForeignKey(g => g.ProductionOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<GoodsProduceBatch>()
                .HasOne(g => g.ProductionOrderLine)
                .WithMany()
                .HasForeignKey(g => g.ProductionOrderLineId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<GoodsProduceBatch>()
                .HasOne(g => g.Material)
                .WithMany()
                .HasForeignKey(g => g.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<GoodsProduceBatch>()
                .HasOne(g => g.Uom)
                .WithMany()
                .HasForeignKey(g => g.UomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodReceiptDocument>()
                .HasIndex(g => g.ProductionOrderId)
                .IsUnique();

            modelBuilder.Entity<GoodReceiptDocument>()
                .HasIndex(g => g.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<GoodReceiptDocument>()
                .Property(g => g.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<GoodReceiptDocument>()
                .Property(g => g.BatchNo)
                .HasMaxLength(64);

            modelBuilder.Entity<GoodReceiptDocument>()
                .HasOne(g => g.ProductionOrder)
                .WithMany()
                .HasForeignKey(g => g.ProductionOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsIssueDocument>()
                .HasIndex(g => g.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<GoodsIssueDocument>()
                .HasIndex(g => g.ProductionOrderId)
                .IsUnique();

            modelBuilder.Entity<GoodsIssueDocument>()
                .Property(g => g.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<GoodsIssueDocument>()
                .Property(g => g.Status)
                .HasMaxLength(20);

            modelBuilder.Entity<GoodsIssueDocument>()
                .Property(g => g.DispatchStatus)
                .HasMaxLength(20)
                .HasDefaultValue(GoodsIssueDocument.DispatchPending);

            modelBuilder.Entity<GoodsIssueDocument>()
                .Property(g => g.DispatchSentByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<GoodsIssueDocument>()
                .HasOne(g => g.ProductionOrder)
                .WithMany()
                .HasForeignKey(g => g.ProductionOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .Property(l => l.RequiredQty)
                .HasPrecision(18, 4);
            
            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .Property(l => l.IssuedQty)
                .HasPrecision(18, 4);
            
            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .Property(l => l.RemainingQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasIndex(l => l.GoodsIssueDocumentId);

            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.GoodsIssueDocument)
                .WithMany(g => g.Lines)
                .HasForeignKey(l => l.GoodsIssueDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.Material)
                .WithMany()
                .HasForeignKey(l => l.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.FertMaterial)
                .WithMany()
                .HasForeignKey(l => l.FertMaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.ProductionOrderLine)
                .WithMany()
                .HasForeignKey(l => l.ProductionOrderLineId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.RequiredUom)
                .WithMany()
                .HasForeignKey(l => l.RequiredUomId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<GoodsIssueDocumentLine>()
                .HasOne(l => l.SelectedBom)
                .WithMany()
                .HasForeignKey(l => l.SelectedBomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(m => m.MovementNumber)
                .IsUnique();

            modelBuilder.Entity<StockMovement>()
                .HasIndex(m => m.MaterialNumber);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(m => m.FromPlantId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(m => m.ToPlantId);

            modelBuilder.Entity<StockMovement>()
                .Property(m => m.QuantityMoved)
                .HasPrecision(18, 4);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Material)
                .WithMany()
                .HasForeignKey(m => m.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.FromPlant)
                .WithMany()
                .HasForeignKey(m => m.FromPlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.ToPlant)
                .WithMany()
                .HasForeignKey(m => m.ToPlantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.QuantityUom)
                .WithMany()
                .HasForeignKey(m => m.QuantityUomId)
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
                .HasIndex(s => new { s.PlantID, s.MaterialNumber, s.QuantityUomId, s.Status, s.Grade, s.BatchOrLot })
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

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .Property(g => g.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .Property(g => g.Status)
                .HasMaxLength(20)
                .HasDefaultValue(SalesGoodsIssueDocument.StatusPending);

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .Property(g => g.DispatchStatus)
                .HasMaxLength(20)
                .HasDefaultValue(SalesGoodsIssueDocument.DispatchPending);

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .Property(g => g.DispatchSentByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .HasIndex(g => g.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .HasIndex(g => g.SalesOrderId)
                .IsUnique();

            modelBuilder.Entity<SalesGoodsIssueDocument>()
                .HasOne(g => g.SalesOrder)
                .WithMany()
                .HasForeignKey(g => g.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(l => l.MaterialNumber)
                .HasMaxLength(32);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(l => l.SalesPriceGrade)
                .HasMaxLength(32);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(l => l.RequiredQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(l => l.IssuedQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .Property(l => l.RemainingQty)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .HasIndex(l => l.SalesGoodsIssueDocumentId);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .HasOne(l => l.SalesGoodsIssueDocument)
                .WithMany(g => g.Lines)
                .HasForeignKey(l => l.SalesGoodsIssueDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .HasOne(l => l.SalesOrderItem)
                .WithMany()
                .HasForeignKey(l => l.SalesOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesGoodsIssueDocumentLine>()
                .HasOne(l => l.RequiredUom)
                .WithMany()
                .HasForeignKey(l => l.RequiredUomId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<SalesInvoice>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesInvoice>()
                .Property(i => i.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesInvoice>()
                .Property(i => i.DcNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesInvoice>()
                .Property(i => i.Subtotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoice>()
                .Property(i => i.GrandTotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoice>()
                .HasIndex(i => i.DeliveryChallanId)
                .IsUnique();

            modelBuilder.Entity<SalesInvoice>()
                .HasIndex(i => i.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesInvoice>()
                .HasOne(i => i.DeliveryChallan)
                .WithMany()
                .HasForeignKey(i => i.DeliveryChallanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesInvoice>()
                .HasOne(i => i.DealerBusinessPartner)
                .WithMany()
                .HasForeignKey(i => i.DealerBusinessPartnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesInvoiceLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesInvoiceLine>()
                .Property(l => l.Quantity)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoiceLine>()
                .Property(l => l.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoiceLine>()
                .Property(l => l.LineSubtotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoiceLine>()
                .Property(l => l.LineTotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesInvoiceLine>()
                .HasOne(l => l.SalesInvoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(l => l.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesInvoiceLine>()
                .HasOne(l => l.QuantityUom)
                .WithMany()
                .HasForeignKey(l => l.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesPayment>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesPayment>()
                .Property(p => p.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesPayment>()
                .Property(p => p.InvoiceDocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesPayment>()
                .HasIndex(p => p.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesPayment>()
                .HasIndex(p => p.SalesInvoiceId)
                .IsUnique();

            modelBuilder.Entity<SalesPayment>()
                .HasOne(p => p.SalesInvoice)
                .WithOne(i => i.SalesPayment)
                .HasForeignKey<SalesPayment>(p => p.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnOrder>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnOrder>()
                .Property(r => r.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnOrder>()
                .Property(r => r.InvoiceDocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnOrder>()
                .Property(r => r.InvoiceGrandTotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrder>()
                .Property(r => r.ItemsDeliveredQuantityTotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrder>()
                .HasIndex(r => r.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesReturnOrder>()
                .HasIndex(r => r.SalesInvoiceId)
                .IsUnique();

            modelBuilder.Entity<SalesReturnOrder>()
                .HasOne(r => r.SalesInvoice)
                .WithOne(i => i.SalesReturnOrder)
                .HasForeignKey<SalesReturnOrder>(r => r.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnOrderLine>()
                .Property(l => l.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .Property(l => l.LineTotal)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .Property(l => l.QuantityInvoiced)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .Property(l => l.QuantityReturned)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .HasOne(l => l.SalesReturnOrder)
                .WithMany(r => r.Lines)
                .HasForeignKey(l => l.SalesReturnOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .HasOne(l => l.SalesInvoiceLine)
                .WithMany()
                .HasForeignKey(l => l.SalesInvoiceLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .HasOne(l => l.QuantityUom)
                .WithMany()
                .HasForeignKey(l => l.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesReturnOrderLine>()
                .HasIndex(l => new { l.SalesReturnOrderId, l.SalesInvoiceLineId })
                .IsUnique();

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .Property(q => q.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .Property(q => q.Status)
                .HasMaxLength(20);

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .Property(q => q.PlantId)
                .HasMaxLength(450);

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .HasIndex(q => q.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .HasIndex(q => q.SalesReturnOrderId)
                .IsUnique();

            modelBuilder.Entity<SalesReturnQualityInspection>()
                .HasOne(q => q.SalesReturnOrder)
                .WithOne(r => r.SalesReturnQualityInspection)
                .HasForeignKey<SalesReturnQualityInspection>(q => q.SalesReturnOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(l => l.QuantityReturned)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(l => l.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(l => l.QtyBackToStock)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(l => l.QtyConvertToRaw)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .Property(l => l.QtyScrap)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .HasOne(l => l.SalesReturnQualityInspection)
                .WithMany(q => q.Lines)
                .HasForeignKey(l => l.SalesReturnQualityInspectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .HasOne(l => l.SalesReturnOrderLine)
                .WithMany()
                .HasForeignKey(l => l.SalesReturnOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnQualityInspectionLine>()
                .HasOne(l => l.QuantityUom)
                .WithMany()
                .HasForeignKey(l => l.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .Property(c => c.DocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .Property(c => c.ReturnOrderDocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .Property(c => c.InvoiceDocumentNumber)
                .HasMaxLength(40);

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .Property(c => c.GrandTotalCredit)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .HasIndex(c => c.DocumentNumber)
                .IsUnique();

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .HasIndex(c => c.SalesReturnOrderId)
                .IsUnique();

            modelBuilder.Entity<SalesReturnCreditMemo>()
                .HasOne(c => c.SalesReturnOrder)
                .WithOne(r => r.SalesReturnCreditMemo)
                .HasForeignKey<SalesReturnCreditMemo>(c => c.SalesReturnOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .Property(l => l.QuantityReturned)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .Property(l => l.UnitPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .Property(l => l.LineCreditAmount)
                .HasPrecision(18, 4);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .HasOne(l => l.SalesReturnCreditMemo)
                .WithMany(c => c.Lines)
                .HasForeignKey(l => l.SalesReturnCreditMemoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .HasOne(l => l.SalesReturnOrderLine)
                .WithMany()
                .HasForeignKey(l => l.SalesReturnOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesReturnCreditMemoLine>()
                .HasOne(l => l.QuantityUom)
                .WithMany()
                .HasForeignKey(l => l.QuantityUomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CompanyInfo>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<CompanyInfo>()
                .Property(c => c.GstOrTaxRate)
                .HasPrecision(18, 4);

            modelBuilder.Entity<CompanyInfo>()
                .HasOne(c => c.UpdatedByUser)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlantsSample>().HasData(
                new PlantsSample { PlantID = "Emp101", PlantName = "Emporium" },
                new PlantsSample { PlantID = "Man102", PlantName = "Manufacturing Plant" }
             
            );
        }
    }
}
