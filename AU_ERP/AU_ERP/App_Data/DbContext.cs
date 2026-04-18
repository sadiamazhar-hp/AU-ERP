using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            modelBuilder.Entity<MaterialNumberRange>()
                .HasOne(r => r.MaterialType)
                .WithMany(t => t.MaterialNumberRanges)
                .HasForeignKey(r => r.MaterialTypeCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaterialNumberRange>()
                .Property(r => r.RangeID)
                .UseIdentityColumn();

            modelBuilder.Entity<UnitConversion>()
                .HasOne(u => u.AltUnit)
                .WithMany()
                .HasForeignKey(u => u.AltUnitId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<BPGrouping>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<DistributionChannel>()
                .Property(e => e.DistributionChannelID)
                .ValueGeneratedOnAdd();

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
                .HasOne(s => s.Material)
                .WithMany()
                .HasForeignKey(s => s.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInventoryLine>()
                .HasOne(s => s.QuantityUom)
                .WithMany()
                .HasForeignKey(s => s.QuantityUomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BOMLevelsSample>().HasData(
                new BOMLevelsSample { LevelID = 1, LevelName = "FG" },
                new BOMLevelsSample { LevelID = 2, LevelName = "SFG" }
            );

            modelBuilder.Entity<PlantsSample>().HasData(
                new PlantsSample { PlantID = "Emp101", PlantName = "Emporium" },
                new PlantsSample { PlantID = "Man102", PlantName = "Manufacturing Plant" }
             
            );
        }
    }
}
