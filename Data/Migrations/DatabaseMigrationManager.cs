using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data.Migrations;

internal sealed class DatabaseMigrationManager(AppDbContext db) : IMigrationManager
{
    public async Task MigrateAsync(CancellationToken ct = default) =>
        await db.Database.MigrateAsync(ct);
}
