using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
public sealed class UserProfileTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private AppDbContext Db =>
        Scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [DockerFact]
    public async Task CreateUser_WithFullName_StoresPartsAndBuildsDisplayName()
    {
        var admin = await CreateAdminTokenAsync();
        var email = $"fio_{Guid.NewGuid():N}@test.local";

        var response = await SendJsonAsync(
            HttpMethod.Post,
            $"{ApiRoutes.PrefixV1}/users",
            new CreateUserRequest(email, null, "Password1", [UserRole.Student], " Иван ", "Петров", "Сергеевич"),
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!;
        created.FirstName.ShouldBe("Иван");
        created.LastName.ShouldBe("Петров");
        created.MiddleName.ShouldBe("Сергеевич");
        created.DisplayName.ShouldBe("Иван Петров");
    }

    [DockerFact]
    public async Task CreateUser_ExplicitDisplayName_IsKept()
    {
        var admin = await CreateAdminTokenAsync();
        var email = $"fio_{Guid.NewGuid():N}@test.local";

        var response = await SendJsonAsync(
            HttpMethod.Post,
            $"{ApiRoutes.PrefixV1}/users",
            new CreateUserRequest(email, "Вася", "Password1", [UserRole.Student], "Иван", "Петров"),
            admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!.DisplayName.ShouldBe("Вася");
    }

    [DockerFact]
    public async Task UpdateProfile_ChangesNamesAndDisplayName_WritesAudit_AndTokenCarriesClaims()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await CreateStudentAsync(admin.AccessToken);

        var update = await SendJsonAsync(
            HttpMethod.Put,
            ProfilePath(target.Id),
            new UpdateUserProfileRequest("Анна", "Смирнова", "Игоревна"),
            admin.AccessToken);

        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var details = await GetDetailsAsync(target.Id, admin.AccessToken);
        details.FirstName.ShouldBe("Анна");
        details.LastName.ShouldBe("Смирнова");
        details.MiddleName.ShouldBe("Игоревна");
        details.DisplayName.ShouldBe("Анна Смирнова");
        details.UpdatedAt.ShouldNotBeNull();

        (await Db.AuditEvents.AnyAsync(item =>
            item.TargetUserId == target.Id && item.EventType == AuditEventTypes.UserProfileUpdated))
            .ShouldBeTrue();

        var login = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(target.Email, "Password1"));
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        var claims = ReadPayload(token.AccessToken);
        claims.GetProperty("given_name").GetString().ShouldBe("Анна");
        claims.GetProperty("family_name").GetString().ShouldBe("Смирнова");
        claims.GetProperty("middle_name").GetString().ShouldBe("Игоревна");
        claims.GetProperty("name").GetString().ShouldBe("Анна Смирнова");
    }

    [DockerFact]
    public async Task Token_WithoutStructuredName_HasNoNameParts()
    {
        var registered = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest($"plain_{Guid.NewGuid():N}@test.local", "Password1", "Без Разбора"));
        registered.EnsureSuccessStatusCode();
        var token = (await registered.Content.ReadFromJsonAsync<TokenResponse>())!;

        var claims = ReadPayload(token.AccessToken);

        claims.TryGetProperty("given_name", out _).ShouldBeFalse();
        claims.TryGetProperty("family_name", out _).ShouldBeFalse();
        claims.GetProperty("name").GetString().ShouldBe("Без Разбора");
    }

    [DockerFact]
    public async Task UpdateProfile_Repeat_IsIdempotent()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await CreateStudentAsync(admin.AccessToken);
        var request = new UpdateUserProfileRequest("Олег", "Громов", null);

        var first = await SendJsonAsync(HttpMethod.Put, ProfilePath(target.Id), request, admin.AccessToken);
        var second = await SendJsonAsync(HttpMethod.Put, ProfilePath(target.Id), request, admin.AccessToken);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Db.AuditEvents.CountAsync(item =>
            item.TargetUserId == target.Id && item.EventType == AuditEventTypes.UserProfileUpdated))
            .ShouldBe(1);
    }

    [DockerFact]
    public async Task UpdateProfile_RequiresAdmin_UnknownUser404_InvalidInput422()
    {
        var admin = await CreateAdminTokenAsync();
        var target = await CreateStudentAsync(admin.AccessToken);
        var student = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest($"stud_{Guid.NewGuid():N}@test.local", "Password1", "Student"));
        var studentToken = (await student.Content.ReadFromJsonAsync<TokenResponse>())!;
        var request = new UpdateUserProfileRequest("Анна", "Смирнова", null);

        var withoutToken = await HttpClient.PutAsJsonAsync(ProfilePath(target.Id), request);
        var asStudent = await SendJsonAsync(HttpMethod.Put, ProfilePath(target.Id), request, studentToken.AccessToken);
        var unknown = await SendJsonAsync(HttpMethod.Put, ProfilePath(Guid.NewGuid()), request, admin.AccessToken);
        var empty = await SendJsonAsync(
            HttpMethod.Put, ProfilePath(target.Id), new UpdateUserProfileRequest(" ", null, "Отч"), admin.AccessToken);
        var tooLong = await SendJsonAsync(
            HttpMethod.Put, ProfilePath(target.Id), new UpdateUserProfileRequest(new string('я', 101), "Ф", null), admin.AccessToken);

        withoutToken.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        asStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        empty.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        tooLong.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task ListUsers_Search_FindsByLastNameAndFirstName()
    {
        var admin = await CreateAdminTokenAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var email = $"find_{marker}@test.local";
        var create = await SendJsonAsync(
            HttpMethod.Post,
            $"{ApiRoutes.PrefixV1}/users",
            new CreateUserRequest(email, null, "Password1", [UserRole.Student], "Имя" + marker, "Фамилия" + marker),
            admin.AccessToken);
        create.EnsureSuccessStatusCode();

        var byLast = await SearchAsync("Фамилия" + marker, admin.AccessToken);
        var byFirst = await SearchAsync("Имя" + marker, admin.AccessToken);

        byLast.Items.ShouldContain(item => item.Email == email && item.LastName == "Фамилия" + marker);
        byFirst.Items.ShouldContain(item => item.Email == email && item.FirstName == "Имя" + marker);
    }

    private async Task<PagedResponse<UserListItemDto>> SearchAsync(string text, string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{ApiRoutes.PrefixV1}/users?search={Uri.EscapeDataString(text)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResponse<UserListItemDto>>())!;
    }

    private async Task<UserDetailsDto> CreateStudentAsync(string adminToken)
    {
        var response = await SendJsonAsync(
            HttpMethod.Post,
            $"{ApiRoutes.PrefixV1}/users",
            new CreateUserRequest($"target_{Guid.NewGuid():N}@test.local", null, "Password1", [UserRole.Student]),
            adminToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!;
    }

    private async Task<UserDetailsDto> GetDetailsAsync(Guid userId, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.PrefixV1}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!;
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

    private async Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string path, T body, string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return await HttpClient.SendAsync(request);
    }

    private static JsonElement ReadPayload(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return JsonDocument.Parse(Convert.FromBase64String(payload)).RootElement.Clone();
    }

    private static string ProfilePath(Guid userId) => $"{ApiRoutes.PrefixV1}/users/{userId}/profile";
}
