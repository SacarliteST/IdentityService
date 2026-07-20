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
public sealed class BlockUserTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private AppDbContext Db =>
        Scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [DockerFact]
    public async Task BlockAndUnblock_RequireAdminRole()
    {
        var student = await RegisterUserAsync();
        var target = await RegisterUserAsync();

        var blockWithoutToken = await HttpClient.PostAsJsonAsync(
            BlockPath(target.UserId),
            new BlockUserRequest(null));
        var unblockWithoutToken = await HttpClient.PostAsync(UnblockPath(target.UserId), null);
        var blockAsStudent = await SendBlockAsync(target.UserId, null, student.AccessToken);
        var unblockAsStudent = await SendUnblockAsync(target.UserId, student.AccessToken);

        blockWithoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unblockWithoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        blockAsStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unblockAsStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task BlockThenUnblock_RevokesSessionsAndRestoresLogin()
    {
        var target = await RegisterUserAsync();
        var admin = await CreateAdminTokenAsync();

        var block = await SendBlockAsync(target.UserId, "Policy violation", admin.AccessToken);
        var repeatedBlock = await SendBlockAsync(target.UserId, "Policy violation", admin.AccessToken);

        block.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        repeatedBlock.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var blockedDetails = await GetDetailsAsync(target.UserId, admin.AccessToken);
        blockedDetails.Status.ShouldBe(UserStatuses.Blocked);
        blockedDetails.BlockedAt.ShouldNotBeNull();
        blockedDetails.BlockReason.ShouldBe("Policy violation");
        blockedDetails.UpdatedAt.ShouldNotBeNull();

        var blockedLogin = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(target.Email, "Password1"));
        var blockedRefresh = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Refresh,
            new RefreshRequest(target.RefreshToken));

        blockedLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        blockedRefresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await Db.RefreshTokens
                .Where(token => token.UserId == target.UserId)
                .ToListAsync())
            .ShouldAllBe(token => token.RevokedAt != null);

        var unblock = await SendUnblockAsync(target.UserId, admin.AccessToken);
        var repeatedUnblock = await SendUnblockAsync(target.UserId, admin.AccessToken);

        unblock.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        repeatedUnblock.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var activeDetails = await GetDetailsAsync(target.UserId, admin.AccessToken);
        activeDetails.Status.ShouldBe(UserStatuses.Active);
        activeDetails.BlockedAt.ShouldBeNull();
        activeDetails.BlockReason.ShouldBeNull();

        var restoredLogin = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(target.Email, "Password1"));
        restoredLogin.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DockerFact]
    public async Task BlockUser_SelfBlock_Returns409()
    {
        var admin = await CreateAdminTokenAsync();

        var response = await SendBlockAsync(admin.UserId, null, admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [DockerFact]
    public async Task BlockUser_LastActiveAdmin_Returns409()
    {
        var actor = await CreateAdminTokenAsync();
        var target = await CreateAdminTokenAsync();
        var admins = await UserManager.GetUsersInRoleAsync(RoleNames.Admin);
        var states = admins
            .Where(admin => admin.Id != target.UserId)
            .Select(admin => new AdminLockoutState(
                admin,
                admin.LockoutEnabled,
                admin.LockoutEnd))
            .ToList();

        try
        {
            foreach (var state in states)
            {
                await Db.Users
                    .Where(user => user.Id == state.User.Id)
                    .ExecuteUpdateAsync(properties => properties
                        .SetProperty(user => user.LockoutEnabled, true)
                        .SetProperty(user => user.LockoutEnd, DateTimeOffset.MaxValue));
            }

            Db.ChangeTracker.Clear();

            var response = await SendBlockAsync(target.UserId, null, actor.AccessToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }
        finally
        {
            foreach (var state in states)
            {
                await Db.Users
                    .Where(user => user.Id == state.User.Id)
                    .ExecuteUpdateAsync(properties => properties
                        .SetProperty(user => user.LockoutEnabled, state.LockoutEnabled)
                        .SetProperty(user => user.LockoutEnd, state.LockoutEnd));
            }

            Db.ChangeTracker.Clear();
        }
    }

    [DockerFact]
    public async Task BlockUser_InvalidReason_Returns422()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await RegisterUserAsync();

        var whitespace = await SendBlockAsync(target.UserId, "   ", admin.AccessToken);
        var tooLong = await SendBlockAsync(target.UserId, new string('x', 501), admin.AccessToken);

        whitespace.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        tooLong.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task BlockAndUnblock_UnknownUser_Return404()
    {
        var admin = await CreateAdminTokenAsync();
        var userId = Guid.NewGuid();

        var block = await SendBlockAsync(userId, null, admin.AccessToken);
        var unblock = await SendUnblockAsync(userId, admin.AccessToken);

        block.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unblock.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<TokenResponse> RegisterUserAsync()
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(
                $"blocked_{Guid.NewGuid():N}@test.local",
                "Password1",
                "Block Target"));
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

        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<HttpResponseMessage> SendBlockAsync(Guid userId, string? reason, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BlockPath(userId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new BlockUserRequest(reason));
        return await HttpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendUnblockAsync(Guid userId, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, UnblockPath(userId));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await HttpClient.SendAsync(request);
    }

    private async Task<UserDetailsDto> GetDetailsAsync(Guid userId, string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{ApiRoutes.PrefixV1}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!;
    }

    private static string BlockPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/block";

    private static string UnblockPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/unblock";

    private sealed record AdminLockoutState(
        ApplicationUser User,
        bool LockoutEnabled,
        DateTimeOffset? LockoutEnd);
}
