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
        public DbSet<BPTypeSample> BPTypeSamples { get; set; }
        public DbSet<BPGrouping> BPGroupings { get; set; }
        public DbSet<BPTypeNumberRanges> BPTypeNumberRanges { get; set; }
        public DbSet<BPNumberRanges> BPNumberRanges { get; set; }
        public DbSet<PlantsSample> PlantsSamples { get; set; }
        public DbSet<WorkCenterMasterSample> WorkCenterMasterSamples { get; set; }
        public DbSet<RoutingHeadersSample> RoutingHeadersSamples { get; set; }
        public DbSet<RoutingOperationsSample> RoutingOperationsSamples { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentRange> DocumentRanges { get; set; }

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

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.BOMLevel)
                .WithMany(l => l.BomHeadersSamples)
                .HasForeignKey(h => h.BLevel)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.MaterialType)
                .WithMany(t => t.BomHeadersSamples)
                .HasForeignKey(h => h.MaterialTypeCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.MaterialMaster)
                .WithMany()
                .HasForeignKey(h => h.MaterialNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BomHeadersSample>()
                .HasOne(h => h.AlternativeBom)
                .WithMany(h => h.DependentBomsWithThisAlternative)
                .HasForeignKey(h => h.AlternativeBOM)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.Role)
                .WithMany(r => r.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPRole)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.TypeSample)
                .WithMany(t => t.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPType)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessPartnerMasterSample>()
                .HasOne(m => m.Grouping)
                .WithMany(g => g.BusinessPartnerMasterSamples)
                .HasForeignKey(m => m.BPGrouping)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BPTypeNumberRanges>()
                .Property(e => e.RangeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPTypeNumberRanges>()
                .HasOne(r => r.TypeSample)
                .WithMany(t => t.BPTypeNumberRanges)
                .HasForeignKey(r => r.BPType)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BPNumberRanges>()
                .Property(e => e.RangeID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<BPNumberRanges>()
                .HasOne(r => r.BusinessPartner)
                .WithMany(m => m.BPNumberRanges)
                .HasForeignKey(r => r.BPID)
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
                .HasOne(o => o.RoutingHeader)
                .WithMany(h => h.RoutingOperationsSamples)
                .HasForeignKey(o => o.RoutingID)
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
        }
    }
}
