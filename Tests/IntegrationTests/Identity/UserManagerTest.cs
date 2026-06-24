using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Identity;

public sealed class UserManagerTest(TestApplication testApplication) : ApiTestBase(testApplication)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [DockerFact]
    public async Task CreateUser_WithValidPassword_Succeeds()
    {
        var email = $"valid_{Guid.NewGuid():N}@test.com";
        var user = new ApplicationUser { UserName = email, Email = email };

        var result = await UserManager.CreateAsync(user, "Valid1Password");

        result.Succeeded.ShouldBeTrue();
    }

    [DockerFact]
    public async Task CreateUser_DuplicateEmail_Fails()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var first = new ApplicationUser { UserName = email, Email = email };
        await UserManager.CreateAsync(first, "Valid1Password");

        var second = new ApplicationUser { UserName = email, Email = email };
        var result = await UserManager.CreateAsync(second, "Valid1Password");

        result.Succeeded.ShouldBeFalse();
    }

    [DockerFact]
    public async Task CreateUser_WeakPassword_Fails()
    {
        var email = $"weak_{Guid.NewGuid():N}@test.com";
        var user = new ApplicationUser { UserName = email, Email = email };

        var result = await UserManager.CreateAsync(user, "weak");

        result.Succeeded.ShouldBeFalse();
    }
}
