using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CreateUserTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private RoleManager<ApplicationRole> RoleManager =>
        Scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    [DockerFact]
    public async Task CreateUser_WithoutToken_Returns401()
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Users.List,
            ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task CreateUser_WithStudentToken_Returns403()
    {
        var student = await RegisterUserAsync();

        var response = await SendCreateAsync(ValidRequest(), student.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DockerFact]
    public async Task CreateUser_AsAdmin_CreatesUserWithCompleteRoleSetWithoutTokens()
    {
        var admin = await CreateAdminTokenAsync();
        var request = ValidRequest([UserRole.Admin, UserRole.Student, UserRole.Teacher]);

        var response = await SendCreateAsync(request, admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("accessToken", out _).ShouldBeFalse();
        document.RootElement.TryGetProperty("refreshToken", out _).ShouldBeFalse();

        var details = JsonSerializer.Deserialize<UserDetailsDto>(json, JsonSerializerOptions.Web);
        details.ShouldNotBeNull();
        details.Email.ShouldBe(request.Email);
        details.DisplayName.ShouldBe(request.DisplayName);
        details.Roles.ShouldBe([RoleNames.Admin, RoleNames.Student, RoleNames.Teacher], ignoreOrder: true);
        details.Status.ShouldBe(UserStatuses.Active);
        details.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        response.Headers.Location!.ToString().ShouldEndWith($"/{details.Id}");

        var user = await UserManager.FindByEmailAsync(request.Email);
        user.ShouldNotBeNull();
        (await UserManager.GetRolesAsync(user))
            .ShouldBe([RoleNames.Admin, RoleNames.Student, RoleNames.Teacher], ignoreOrder: true);

        var adminStillAuthorized = await GetWithTokenAsync(ApiRoutes.Users.List, admin.AccessToken);
        adminStillAuthorized.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DockerFact]
    public async Task CreateUser_DuplicateEmail_Returns409WithoutSecondUser()
    {
        var admin = await CreateAdminTokenAsync();
        var request = ValidRequest();
        (await SendCreateAsync(request, admin.AccessToken)).EnsureSuccessStatusCode();

        var duplicate = await SendCreateAsync(request, admin.AccessToken);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        UserManager.Users.Count(user => user.NormalizedEmail == request.Email.ToUpperInvariant())
            .ShouldBe(1);
    }

    [DockerFact]
    public async Task CreateUser_InvalidPayloads_Return422()
    {
        var admin = await CreateAdminTokenAsync();
        var emailPrefix = Guid.NewGuid().ToString("N");
        object[] invalidRequests =
        [
            new CreateUserRequest("not-an-email", "User", "Password1", [UserRole.Student]),
            new CreateUserRequest($"weak_{emailPrefix}@test.local", "User", "password", [UserRole.Student]),
            new CreateUserRequest($"name_{emailPrefix}@test.local", "   ", "Password1", [UserRole.Student]),
            new CreateUserRequest($"empty_{emailPrefix}@test.local", "User", "Password1", []),
            new CreateUserRequest(
                $"duplicate_{emailPrefix}@test.local",
                "User",
                "Password1",
                [UserRole.Student, UserRole.Student]),
            new
            {
                email = $"role_{emailPrefix}@test.local",
                displayName = "User",
                password = "Password1",
                roles = new[] { 999 }
            }
        ];

        foreach (var invalidRequest in invalidRequests)
        {
            var response = await SendCreateAsync(invalidRequest, admin.AccessToken);

            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problem.ShouldNotBeNull();
            problem.Errors.ShouldNotBeEmpty();
        }
    }

    [DockerFact]
    public async Task CreateUser_WhenRoleAssignmentFails_RollsBackCreatedUser()
    {
        var admin = await CreateAdminTokenAsync();
        var request = ValidRequest([UserRole.Teacher]);
        var teacherRole = await RoleManager.FindByNameAsync(RoleNames.Teacher);
        teacherRole.ShouldNotBeNull();
        teacherRole.Name = "UnavailableTeacher";
        (await RoleManager.UpdateAsync(teacherRole)).Succeeded.ShouldBeTrue();

        HttpResponseMessage response;
        try
        {
            response = await SendCreateAsync(request, admin.AccessToken);
        }
        finally
        {
            teacherRole.Name = RoleNames.Teacher;
            (await RoleManager.UpdateAsync(teacherRole)).Succeeded.ShouldBeTrue();
        }

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await UserManager.FindByEmailAsync(request.Email)).ShouldBeNull();
    }

    private static CreateUserRequest ValidRequest(IReadOnlyList<UserRole>? roles = null) =>
        new(
            $"created_{Guid.NewGuid():N}@test.local",
            "Created User",
            "Password1",
            roles ?? [UserRole.Student]);

    private async Task<TokenResponse> RegisterUserAsync()
    {
        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(
                $"student_{Guid.NewGuid():N}@test.local",
                "Password1",
                "Student"));
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

    private async Task<HttpResponseMessage> SendCreateAsync(object request, string token)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Users.List);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Content = JsonContent.Create(request);
        return await HttpClient.SendAsync(message);
    }

    private async Task<HttpResponseMessage> GetWithTokenAsync(string path, string token)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await HttpClient.SendAsync(message);
    }
}
