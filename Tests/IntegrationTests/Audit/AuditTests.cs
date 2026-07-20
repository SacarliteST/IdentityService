using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Audit;

[Collection(nameof(IntegrationTestCollection))]
public sealed class AuditTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [DockerFact]
    public async Task AuditEndpoints_RequireAdminRole()
    {
        var student = await RegisterUserAsync();

        var auditWithoutToken = await HttpClient.GetAsync(ApiRoutes.Audit.List);
        var activityWithoutToken = await HttpClient.GetAsync(ActivityPath(student.UserId));
        var auditAsStudent = await GetWithTokenAsync(ApiRoutes.Audit.List, student.AccessToken);
        var activityAsStudent = await GetWithTokenAsync(
            ActivityPath(student.UserId),
            student.AccessToken);

        auditWithoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        activityWithoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        auditAsStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        activityAsStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task AuditWorkflow_ReturnsFilteredPagedEventsWithoutSecrets()
    {
        var from = DateTimeOffset.UtcNow.AddSeconds(-1);
        var admin = await CreateAdminTokenAsync();
        const string password = "Password1";
        var email = $"audit_{Guid.NewGuid():N}@test.local";

        var createResponse = await SendWithTokenAsync(
            HttpMethod.Post,
            ApiRoutes.Users.List,
            new CreateUserRequest(email, "Audit Target", password, [UserRole.Student]),
            admin.AccessToken);
        createResponse.EnsureSuccessStatusCode();
        var target = (await createResponse.Content.ReadFromJsonAsync<UserDetailsDto>())!;

        (await SendWithTokenAsync(
            HttpMethod.Put,
            RolesPath(target.Id),
            new UpdateUserRolesRequest([UserRole.Teacher]),
            admin.AccessToken)).EnsureSuccessStatusCode();
        (await SendWithTokenAsync(
            HttpMethod.Post,
            BlockPath(target.Id),
            new BlockUserRequest("Audit reason"),
            admin.AccessToken)).EnsureSuccessStatusCode();
        (await SendWithTokenAsync(
            HttpMethod.Post,
            UnblockPath(target.Id),
            null,
            admin.AccessToken)).EnsureSuccessStatusCode();

        var failedLogin = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(email, "WrongPassword1"));
        failedLogin.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var loginResponse = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var targetTokens = (await loginResponse.Content.ReadFromJsonAsync<TokenResponse>())!;
        var to = DateTimeOffset.UtcNow.AddSeconds(1);

        var activityResponse = await GetWithTokenAsync(
            $"{ActivityPath(target.Id)}?page=1&pageSize=20",
            admin.AccessToken);
        activityResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var activity = (await activityResponse.Content
            .ReadFromJsonAsync<PagedResponse<AuditEventDto>>())!;

        activity.TotalCount.ShouldBe(5);
        activity.Items.Select(item => item.EventType).ShouldBe(
            [
                AuditEventTypes.LoginSucceeded,
                AuditEventTypes.UserUnblocked,
                AuditEventTypes.UserBlocked,
                AuditEventTypes.UserRolesUpdated,
                AuditEventTypes.UserCreated
            ],
            ignoreOrder: true);
        activity.Items.ShouldBe(activity.Items
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id));
        activity.Items.Single(item => item.EventType == AuditEventTypes.LoginSucceeded)
            .ActorUserId.ShouldBe(target.Id);
        activity.Items
            .Where(item => item.EventType != AuditEventTypes.LoginSucceeded)
            .ShouldAllBe(item => item.ActorUserId == admin.UserId);

        foreach (var auditEvent in activity.Items)
        {
            auditEvent.Description.ShouldNotContain(password);
            auditEvent.Description.ShouldNotContain(admin.AccessToken);
            auditEvent.Description.ShouldNotContain(targetTokens.RefreshToken);
        }

        var filter = $"{ApiRoutes.Audit.List}?page=1&pageSize=1" +
            $"&actorUserId={admin.UserId}&targetUserId={target.Id}" +
            $"&eventType={AuditEventTypes.UserBlocked}" +
            $"&from={Uri.EscapeDataString(from.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("O"))}";
        var filteredResponse = await GetWithTokenAsync(filter, admin.AccessToken);
        var filtered = (await filteredResponse.Content
            .ReadFromJsonAsync<PagedResponse<AuditEventDto>>())!;

        filteredResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        filtered.Page.ShouldBe(1);
        filtered.PageSize.ShouldBe(1);
        filtered.TotalCount.ShouldBe(1);
        filtered.Items.Single().EventType.ShouldBe(AuditEventTypes.UserBlocked);
        filtered.Items.Single().TargetUserId.ShouldBe(target.Id);
    }

    [DockerFact]
    public async Task AuditEndpoints_InvalidQueryAndUnknownUser_ReturnExpectedErrors()
    {
        var admin = await CreateAdminTokenAsync();
        var invalidQueries = new[]
        {
            "?page=0",
            "?pageSize=101",
            "?eventType=Unknown",
            "?from=2026-07-21T00%3A00%3A00Z&to=2026-07-20T00%3A00%3A00Z"
        };

        foreach (var query in invalidQueries)
        {
            var response = await GetWithTokenAsync(
                ApiRoutes.Audit.List + query,
                admin.AccessToken);
            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }

        var unknownUser = await GetWithTokenAsync(
            ActivityPath(Guid.NewGuid()),
            admin.AccessToken);
        unknownUser.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<TokenResponse> RegisterUserAsync()
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(
                $"audit_student_{Guid.NewGuid():N}@test.local",
                "Password1",
                "Audit Student"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<TokenResponse> CreateAdminTokenAsync()
    {
        var email = $"audit_admin_{Guid.NewGuid():N}@test.local";
        const string password = "Admin1234";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            DisplayName = "Audit Admin",
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

    private async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpMethod method,
        string path,
        object? body,
        string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await HttpClient.SendAsync(request);
    }

    private static string ActivityPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/activity";

    private static string RolesPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/roles";

    private static string BlockPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/block";

    private static string UnblockPath(Guid userId) =>
        $"{ApiRoutes.PrefixV1}/users/{userId}/unblock";
}
