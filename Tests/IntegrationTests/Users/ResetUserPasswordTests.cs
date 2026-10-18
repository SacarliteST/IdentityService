using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ResetUserPasswordTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private AppDbContext Db =>
        Scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [DockerFact]
    public async Task ResetPassword_RequiresAdminRole()
    {
        var student = await RegisterUserAsync();
        var target = await RegisterUserAsync();

        var withoutToken = await HttpClient.PutAsJsonAsync(PasswordPath(target.UserId), new ResetUserPasswordRequest("NewPass123"));
        var asStudent = await SendAsync(target.UserId, "NewPass123", student.AccessToken);

        withoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        asStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task ResetPassword_ChangesPassword_RevokesSessions_AndWritesAudit()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();

        var response = await SendAsync(target.UserId, "NewPass123", admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var oldLogin = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(target.Email, "Password1"));
        var newLogin = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(target.Email, "NewPass123"));
        var oldRefresh = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(target.RefreshToken));
        oldLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
        oldRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await Db.AuditEvents.AnyAsync(item =>
            item.TargetUserId == target.UserId && item.EventType == AuditEventTypes.UserPasswordReset))
            .ShouldBeTrue();
    }

    [DockerFact]
    public async Task ResetPassword_WeakPassword_Returns422()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();

        var response = await SendAsync(target.UserId, "short", admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task ResetPassword_UnknownUser_Returns404()
    {
        var admin = await CreateAdminTokenAsync();

        var response = await SendAsync(Guid.NewGuid(), "NewPass123", admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<TokenResponse> RegisterUserAsync()
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest($"reset_{Guid.NewGuid():N}@test.local", "Password1", "Reset Target"));
        response.EnsureSuccessStatusCode();
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
            DisplayName = "Integration Admin",
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        (await UserManager.CreateAsync(user, password)).Succeeded.ShouldBeTrue();
        (await UserManager.AddToRoleAsync(user, RoleNames.Admin)).Succeeded.ShouldBeTrue();

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<HttpResponseMessage> SendAsync(Guid userId, string newPassword, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, PasswordPath(userId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new ResetUserPasswordRequest(newPassword));
        return await HttpClient.SendAsync(request);
    }

    private static string PasswordPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/password";
}
