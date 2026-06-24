using IdentityService.Host;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityService.IntegrationTests.Infrastructure;

/// <summary>
/// Тестовый хост. В промте A: добавить PostgreSqlContainer + ConfigureWebHost с конфигом подключения.
/// </summary>
public sealed class TestApplication : WebApplicationFactory<IHostMarker>, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
