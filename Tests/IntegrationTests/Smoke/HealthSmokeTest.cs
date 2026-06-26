using System.Net;
using IdentityService.IntegrationTests.Infrastructure;
using Shouldly;

namespace IdentityService.IntegrationTests.Smoke;

public sealed class HealthSmokeTest(TestApplication testApplication) : ApiTestBase(testApplication)
{
    [DockerFact]
    public async Task Get_Health_Returns200()
    {
        var response = await HttpClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
