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
    internal const string TestDbName = "identity_test";
    internal const string TestUser = "identity_test_user";
    private const string TestPassword = "identity_test_password";
    internal const string TestIssuer = "https://identity.test";
    internal const string TestAudience = "identity.api";

    private PostgreSqlContainer? postgres = null;
    private string? connectionString = null;

    public async Task InitializeAsync()
    {
        postgres = new PostgreSqlBuilder()
            .WithDatabase(TestDbName)
            .WithUsername(TestUser)
            .WithPassword(TestPassword)
            .WithCleanUp(true)
            .Build();

        await postgres.StartAsync();
        connectionString = postgres.GetConnectionString();

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<IMigrationManager>().MigrateAsync();
        await RoleSeeder.SeedRolesAsync(sp.GetRequiredService<RoleManager<ApplicationRole>>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var overrides = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience },
            { "ConnectionStrings:ConnectionString", connectionString }
        };

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(overrides));

        base.ConfigureWebHost(builder);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (postgres is not null)
        {
            await postgres.DisposeAsync();
        }
    }
}
