using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class ApiTestBase
{
    protected readonly HttpClient HttpClient;
    protected readonly IServiceScope Scope;

    protected ApiTestBase(TestApplication testApplication)
    {
        HttpClient = testApplication.CreateClient();
        Scope = testApplication.Services.CreateScope();
    }
}
