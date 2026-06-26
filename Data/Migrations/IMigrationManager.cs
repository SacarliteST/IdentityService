namespace IdentityService.Data.Migrations;

/// <summary>
/// Применяет pending EF Core миграции к базе данных.
/// Вызывается при старте приложения до обработки запросов.
/// </summary>
public interface IMigrationManager
{
    /// <summary>Применяет все незапущенные миграции.</summary>
    Task MigrateAsync(CancellationToken ct = default);
}
