using AU_ERP.Models;
using Microsoft.EntityFrameworkCore;

namespace AU_ERP.Services;

/// <summary>Ensures configuration lookup tables used by material master have baseline rows (no-op if data already exists).</summary>
public static class ConfigurationLookupSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.MaterialGroups.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.MaterialGroups.AddRange(
                new MaterialGroup { MaterialGroupCode = "ROH", Description = "Raw materials" },
                new MaterialGroup { MaterialGroupCode = "HALB", Description = "Semifinished products" },
                new MaterialGroup { MaterialGroupCode = "FERT", Description = "Finished products" },
                new MaterialGroup { MaterialGroupCode = "PACK", Description = "Packaging" }
            );
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.PlantsSamples.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.PlantsSamples.AddRange(
                new PlantsSample { PlantID = "Emp101", PlantName = "Emporium" },
                new PlantsSample { PlantID = "Man102", PlantName = "Manufacturing plant" }
            );
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
