namespace IdentityService.Data.Migrations;

public interface IMigrationManager
{
    Task MigrateAsync(CancellationToken ct = default);
}
