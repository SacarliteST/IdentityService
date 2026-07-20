using IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Infrastructure;

public sealed class TestDatabaseIsolationTests(TestApplication app) : ApiTestBase(app)
{
    [DockerFact]
    public async Task IntegrationTests_UseContainerDatabase()
    {
        var db = Scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = db.Database.GetDbConnection();

        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT current_database(), current_user";

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        reader.GetString(0).ShouldBe(TestApplication.TestDbName);
        reader.GetString(1).ShouldBe(TestApplication.TestUser);
    }
}
