using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Identity;

public sealed class RolesSeededTest(TestApplication testApplication) : ApiTestBase(testApplication)
{
    [DockerTheory]
    [InlineData(RoleNames.Student)]
    [InlineData(RoleNames.Teacher)]
    [InlineData(RoleNames.Admin)]
    public async Task Role_ExistsAfterSeed(string roleName)
    {
        var roleManager = Scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var exists = await roleManager.RoleExistsAsync(roleName);

        exists.ShouldBeTrue();
    }
}
