using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data.Migrations;

/// <summary>
/// Реализация <see cref="IMigrationManager"/> через EF Core.
/// Вызывает <c>Database.MigrateAsync</c>, которая применяет все pending-миграции
/// и создаёт БД, если она не существует.
/// </summary>
internal sealed class DatabaseMigrationManager(AppDbContext db) : IMigrationManager
{
    /// <inheritdoc/>
    public async Task MigrateAsync(CancellationToken ct = default) =>
        await db.Database.MigrateAsync(ct);
}
