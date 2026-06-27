using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class AssignRoleTests(TestApplication app) : ApiTestBase(app)
{
    private async Task<TokenResponse> RegisterUserAsync(string? suffix = null)
    {
        var email = $"ar_{suffix ?? Guid.NewGuid().ToString()}@test.com";
        var reg = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null));
        return (await reg.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<TokenResponse> CreateAdminTokenAsync()
    {
        // Create an admin user manually via UserManager
        var email = $"admin_{Guid.NewGuid()}@test.com";
        const string password = "Admin1234!";

        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Email = email, UserName = email };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, RoleNames.Admin);

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest(email, password));
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    [DockerFact]
    public async Task AssignRole_WithoutToken_Returns401()
    {
        var target = await RegisterUserAsync();
        var path = $"{ApiRoutes.PrefixV1}/users/{target.UserId}/roles";

        var response = await HttpClient.PostAsJsonAsync(path,
            new AssignRoleRequest(RoleNames.Teacher));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task AssignRole_NonAdminToken_Returns403()
    {
        var student = await RegisterUserAsync();
        var target = await RegisterUserAsync();
        var path = $"{ApiRoutes.PrefixV1}/users/{target.UserId}/roles";

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", student.AccessToken);
        req.Content = JsonContent.Create(new AssignRoleRequest(RoleNames.Teacher));

        var response = await HttpClient.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task AssignRole_AdminToken_Returns204_UserHasRole()
    {
        var adminTokens = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();
        var path = $"{ApiRoutes.PrefixV1}/users/{target.UserId}/roles";

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        req.Content = JsonContent.Create(new AssignRoleRequest(RoleNames.Teacher));

        var response = await HttpClient.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the role was assigned
        var loginResp = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest(target.Email, "Password1!"));
        var refreshed = (await loginResp.Content.ReadFromJsonAsync<TokenResponse>())!;
        refreshed.Roles.ShouldContain(RoleNames.Teacher);
    }

    [DockerFact]
    public async Task AssignRole_UnknownRole_Returns409()
    {
        var adminTokens = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();
        var path = $"{ApiRoutes.PrefixV1}/users/{target.UserId}/roles";

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        req.Content = JsonContent.Create(new AssignRoleRequest("NonExistentRole"));

        var response = await HttpClient.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [DockerFact]
    public async Task AssignRole_UnknownUser_Returns404()
    {
        var adminTokens = await CreateAdminTokenAsync();
        var path = $"{ApiRoutes.PrefixV1}/users/{Guid.NewGuid()}/roles";

        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        req.Content = JsonContent.Create(new AssignRoleRequest(RoleNames.Teacher));

        var response = await HttpClient.SendAsync(req);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
