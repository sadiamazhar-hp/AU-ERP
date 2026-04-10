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
    }
}