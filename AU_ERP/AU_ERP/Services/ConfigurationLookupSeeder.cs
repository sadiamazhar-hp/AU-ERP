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

        // Material Groups are intentionally NOT seeded.
        // Material Types are seeded via EF model seed/migrations.

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
