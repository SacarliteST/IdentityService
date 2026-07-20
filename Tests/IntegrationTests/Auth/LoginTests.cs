using System.Net;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Auth;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LoginTests(TestApplication app) : ApiTestBase(app)
{
    [DockerFact]
    public async Task Login_CorrectCredentials_Returns200WithTokens()
    {
        var email = $"login_{Guid.NewGuid()}@test.com";
        const string password = "Password1!";

        await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, password, null));

        var loginStartedAt = DateTimeOffset.UtcNow;
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest(email, password));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
        body.RefreshToken.ShouldNotBeNullOrEmpty();
        body.Roles.ShouldContain(RoleNames.Student);

        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        user.LastLoginAt.ShouldNotBeNull();
        user.LastLoginAt.Value.ShouldBeGreaterThanOrEqualTo(loginStartedAt);
        user.LastLoginAt.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [DockerFact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrongpwd_{Guid.NewGuid()}@test.com";
        await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null));

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest(email, "WrongPassword99!"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        user.LastLoginAt.ShouldBeNull();
    }

    [DockerFact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest("nobody@nowhere.com", "Password1!"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
