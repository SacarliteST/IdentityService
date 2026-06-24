namespace IdentityService.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class ApiTestBase
{
    protected readonly HttpClient HttpClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        HttpClient = testApplication.CreateClient();
    }
}
