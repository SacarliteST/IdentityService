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
public sealed class UpdateUserRolesTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [DockerFact]
    public async Task UpdateRoles_WithoutToken_Returns401()
    {
        var target = await RegisterUserAsync();

        var response = await HttpClient.PutAsJsonAsync(
            RolesPath(target.UserId),
            new UpdateUserRolesRequest([UserRole.Teacher]));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task UpdateRoles_NonAdminToken_Returns403()
    {
        var student = await RegisterUserAsync();
        var target = await RegisterUserAsync();

        var response = await SendUpdateAsync(
            target.UserId,
            [UserRole.Teacher],
            student.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task UpdateRoles_AdminToken_ReplacesCompleteRoleSet()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();

        var response = await SendUpdateAsync(
            target.UserId,
            [UserRole.Teacher],
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var login = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(target.Email, "Password1!"));
        var refreshed = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        refreshed.Roles.ShouldBe([RoleNames.Teacher], ignoreOrder: true);

        var user = await UserManager.FindByIdAsync(target.UserId.ToString());
        user.ShouldNotBeNull();
        user.UpdatedAt.ShouldNotBeNull();
    }

    [DockerFact]
    public async Task UpdateRoles_SameRoleSet_IsIdempotent()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();

        var response = await SendUpdateAsync(
            target.UserId,
            [UserRole.Student],
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [DockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateRoles_InvalidRoleSet_Returns422(bool duplicate)
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();
        IReadOnlyList<UserRole> roles = duplicate
            ? [UserRole.Student, UserRole.Student]
            : [];

        var response = await SendUpdateAsync(target.UserId, roles, admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("Roles");
    }

    [DockerFact]
    public async Task UpdateRoles_UnknownUser_Returns404()
    {
        var admin = await CreateAdminTokenAsync();

        var response = await SendUpdateAsync(
            Guid.NewGuid(),
            [UserRole.Teacher],
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [DockerFact]
    public async Task UpdateRoles_AdminCannotRemoveOwnAdminRole()
    {
        var admin = await CreateAdminTokenAsync();

        var response = await SendUpdateAsync(
            admin.UserId,
            [UserRole.Student],
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [DockerFact]
    public async Task AssignRole_LegacyPostEndpoint_Returns405()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, RolesPath(target.UserId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        request.Content = JsonContent.Create(new UpdateUserRolesRequest([UserRole.Teacher]));

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    private async Task<TokenResponse> RegisterUserAsync()
    {
        var email = $"roles_{Guid.NewGuid():N}@test.local";
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null));
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<TokenResponse> CreateAdminTokenAsync()
    {
        var email = $"admin_{Guid.NewGuid():N}@test.local";
        const string password = "Admin1234";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        (await UserManager.CreateAsync(user, password)).Succeeded.ShouldBeTrue();
        (await UserManager.AddToRoleAsync(user, RoleNames.Admin)).Succeeded.ShouldBeTrue();

        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(email, password));
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<HttpResponseMessage> SendUpdateAsync(
        Guid userId,
        IReadOnlyList<UserRole> roles,
        string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, RolesPath(userId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new UpdateUserRolesRequest(roles));
        return await HttpClient.SendAsync(request);
    }

    private static string RolesPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/roles";
}
