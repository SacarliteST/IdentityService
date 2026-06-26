using IdentityService.Data.Migrations;
using IdentityService.Data.Seeding;
using IdentityService.Domain;
using IdentityService.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace IdentityService.IntegrationTests.Infrastructure;

public sealed class TestApplication : WebApplicationFactory<IHostMarker>, IAsyncLifetime
{
    private const string TestDbName = "identity_test";
    private const string TestUser = "identity_test_user";
    private const string TestPassword = "identity_test_password";

    // Инициализируются в InitializeAsync, чтобы конструктор не обращался к Docker.
    private PostgreSqlContainer? postgres = null;
    private string? connectionString = null;

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        postgres = new PostgreSqlBuilder()
            .WithDatabase(TestDbName)
            .WithUsername(TestUser)
            .WithPassword(TestPassword)
            .WithCleanUp(true)
            .Build();

        await postgres.StartAsync();
        connectionString = postgres.GetConnectionString();

        // Services тригерит ConfigureWebHost — вызываем после установки _connectionString.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<IMigrationManager>().MigrateAsync();
        await RoleSeeder.SeedRolesAsync(sp.GetRequiredService<RoleManager<ApplicationRole>>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (connectionString is not null)
        {
            builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:ConnectionString", connectionString }
                }));
        }

        base.ConfigureWebHost(builder);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (postgres is not null)
        {
            await postgres.StopAsync();
        }
    }
}
