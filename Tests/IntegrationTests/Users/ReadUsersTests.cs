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
public sealed class ReadUsersTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [DockerFact]
    public async Task ListUsers_WithoutToken_Returns401()
    {
        var response = await HttpClient.GetAsync(ApiRoutes.Users.List);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task ListUsers_WithStudentToken_Returns403()
    {
        var student = await RegisterUserAsync($"student_{Guid.NewGuid():N}@test.local", "Student");

        var response = await GetWithTokenAsync(ApiRoutes.Users.List, student.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task ListUsers_FiltersAndPaginates_ReturnsMatchingUsersInStableOrder()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var first = await RegisterUserAsync($"a_{suffix}@test.local", $"Search {suffix}");
        var second = await RegisterUserAsync($"b_{suffix}@test.local", $"Search {suffix}");
        await UserManager.AddToRoleAsync(
            (await UserManager.FindByIdAsync(first.UserId.ToString()))!,
            RoleNames.Teacher);
        await UserManager.AddToRoleAsync(
            (await UserManager.FindByIdAsync(second.UserId.ToString()))!,
            RoleNames.Teacher);
        var admin = await CreateAdminTokenAsync();

        var query = $"{ApiRoutes.Users.List}?page=1&pageSize=1" +
            $"&search={Uri.EscapeDataString(suffix)}&role=teacher&status=active";
        var response = await GetWithTokenAsync(query, admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<UserListItemDto>>();
        page.ShouldNotBeNull();
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(1);
        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        page.Items[0].Email.ShouldBe(first.Email);
        page.Items[0].Roles.ShouldContain(RoleNames.Teacher);
        page.Items[0].Status.ShouldBe(UserStatuses.Active);
    }

    [DockerFact]
    public async Task ListUsers_BlockedFilter_ReturnsBlockedUserMetadata()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var registered = await RegisterUserAsync($"blocked_{suffix}@test.local", $"Blocked {suffix}");
        var user = await UserManager.FindByIdAsync(registered.UserId.ToString());
        user.ShouldNotBeNull();
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.BlockedAt = DateTimeOffset.UtcNow;
        user.BlockReason = "Integration test";
        (await UserManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        var admin = await CreateAdminTokenAsync();

        var query = $"{ApiRoutes.Users.List}?search={Uri.EscapeDataString(suffix)}&status=Blocked";
        var response = await GetWithTokenAsync(query, admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<UserListItemDto>>();
        page.ShouldNotBeNull();
        page.TotalCount.ShouldBe(1);
        page.Items.Single().Id.ShouldBe(registered.UserId);
        page.Items.Single().Status.ShouldBe(UserStatuses.Blocked);
    }

    [DockerTheory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?role=Unknown")]
    [InlineData("?status=Unknown")]
    public async Task ListUsers_InvalidQuery_Returns422(string query)
    {
        var admin = await CreateAdminTokenAsync();

        var response = await GetWithTokenAsync($"{ApiRoutes.Users.List}{query}", admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task GetUserDetails_AsAdmin_ReturnsUserCard()
    {
        var registered = await RegisterUserAsync(
            $"details_{Guid.NewGuid():N}@test.local",
            "Details User");
        var target = await UserManager.FindByIdAsync(registered.UserId.ToString());
        target.ShouldNotBeNull();
        await UserManager.AddToRoleAsync(target, RoleNames.Teacher);
        var admin = await CreateAdminTokenAsync();

        var response = await GetWithTokenAsync(
            $"{ApiRoutes.PrefixV1}/users/{registered.UserId}",
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<UserDetailsDto>();
        details.ShouldNotBeNull();
        details.Id.ShouldBe(registered.UserId);
        details.Email.ShouldBe(registered.Email);
        details.DisplayName.ShouldBe("Details User");
        details.Roles.ShouldContain(RoleNames.Student);
        details.Roles.ShouldContain(RoleNames.Teacher);
        details.Status.ShouldBe(UserStatuses.Active);
        details.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        details.UpdatedAt.ShouldBeNull();
        details.BlockedAt.ShouldBeNull();
        details.BlockReason.ShouldBeNull();
    }

    [DockerFact]
    public async Task GetUserDetails_UnknownUser_Returns404()
    {
        var admin = await CreateAdminTokenAsync();

        var response = await GetWithTokenAsync(
            $"{ApiRoutes.PrefixV1}/users/{Guid.NewGuid()}",
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<TokenResponse> RegisterUserAsync(string email, string displayName)
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", displayName));

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

    private async Task<HttpResponseMessage> GetWithTokenAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await HttpClient.SendAsync(request);
    }
}
