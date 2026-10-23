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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegistrationAndCabinetTests : ApiTestBase
{
    private readonly TestApplication testApplication;

    public RegistrationAndCabinetTests(TestApplication app)
        : base(app)
    {
        testApplication = app;
    }

    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    private AppDbContext Db => Scope.ServiceProvider.GetRequiredService<AppDbContext>();

    /// <summary>Клиент хоста с заданными настройками (поверх тестовых: Registration:Mode=Open и широкий лимит).</summary>
    private HttpClient ClientWith(params (string Key, string Value)[] settings) =>
        testApplication.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(settings.ToDictionary(item => item.Key, item => (string?)item.Value))))
            .CreateClient();

    private HttpClient ApprovalClient() => ClientWith(("Registration:Mode", "Approval"));

    // ---------- регистрация и одобрение ----------

    [DockerFact]
    public async Task Approval_RegisterWithoutCode_IsPending_CannotLogin_UntilApproved()
    {
        var client = ApprovalClient();
        var admin = await CreateAdminTokenAsync();
        var email = NewEmail("pending");

        var register = await client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1", null, "Иван", "Петров", "Сергеевич"));

        register.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var pending = (await register.Content.ReadFromJsonAsync<RegisterPendingResponse>())!;
        pending.Status.ShouldBe(UserStatuses.Pending);

        var loginPending = await client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1"));
        loginPending.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ProblemCodeAsync(loginPending)).ShouldBe("ApplicationUser.AccountPendingApproval");

        var user = await FindUserAsync(email);
        var details = await GetDetailsAsync(user.Id, admin.AccessToken);
        details.Status.ShouldBe(UserStatuses.Pending);
        details.DisplayName.ShouldBe("Иван Петров");
        (await Db.AuditEvents.AnyAsync(item =>
            item.TargetUserId == user.Id && item.EventType == AuditEventTypes.UserRegistered)).ShouldBeTrue();

        var approve = await SendAsync(HttpMethod.Post, ApprovePath(user.Id), admin.AccessToken);
        var approveAgain = await SendAsync(HttpMethod.Post, ApprovePath(user.Id), admin.AccessToken);
        approve.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        approveAgain.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var loginApproved = await client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1"));
        loginApproved.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetDetailsAsync(user.Id, admin.AccessToken)).Status.ShouldBe(UserStatuses.Active);
        (await Db.AuditEvents.CountAsync(item =>
            item.TargetUserId == user.Id && item.EventType == AuditEventTypes.UserApproved)).ShouldBe(1);
    }

    [DockerFact]
    public async Task Reject_BlocksLogin_CanBeApprovedLater_ButApprovedCannotBeRejected()
    {
        var client = ApprovalClient();
        var admin = await CreateAdminTokenAsync();
        var email = NewEmail("rejected");
        (await client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1", "Отклонённый")))
            .StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var user = await FindUserAsync(email);

        var reject = await SendJsonAsync(HttpMethod.Post, RejectPath(user.Id), new RejectUserRequest("Не из нашей группы"), admin.AccessToken);

        reject.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var login = await client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1"));
        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ProblemCodeAsync(login)).ShouldBe("ApplicationUser.AccountRejected");
        (await GetDetailsAsync(user.Id, admin.AccessToken)).Status.ShouldBe(UserStatuses.Rejected);
        (await Db.AuditEvents.AnyAsync(item =>
            item.TargetUserId == user.Id && item.Description.Contains("Не из нашей группы"))).ShouldBeTrue();

        (await SendAsync(HttpMethod.Post, ApprovePath(user.Id), admin.AccessToken))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var rejectApproved = await SendJsonAsync(HttpMethod.Post, RejectPath(user.Id), new RejectUserRequest(null), admin.AccessToken);
        rejectApproved.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [DockerFact]
    public async Task ApproveAndReject_RequireAdmin_UnknownUserIs404()
    {
        var admin = await CreateAdminTokenAsync();
        var student = await RegisterOpenAsync(NewEmail("stud"));

        var asStudent = await SendAsync(HttpMethod.Post, ApprovePath(Guid.NewGuid()), student.AccessToken);
        var unknown = await SendAsync(HttpMethod.Post, ApprovePath(Guid.NewGuid()), admin.AccessToken);

        asStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [DockerFact]
    public async Task PendingFilter_ListsOnlyPendingUsers()
    {
        var client = ApprovalClient();
        var admin = await CreateAdminTokenAsync();
        var email = NewEmail("listed");
        await client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1", "В списке"));

        var response = await SendAsync(HttpMethod.Get, $"{ApiRoutes.PrefixV1}/users?status=Pending&pageSize=100", admin.AccessToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResponse<UserListItemDto>>())!;
        page.Items.ShouldContain(item => item.Email == email);
        page.Items.ShouldAllBe(item => item.Status == UserStatuses.Pending);
    }

    [DockerFact]
    public async Task OpenMode_RegistersWithTokens_ClosedMode_Conflicts()
    {
        var open = await ClientWith(("Registration:Mode", "Open"))
            .PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(NewEmail("open"), "Password1", "Открытый"));
        var closed = await ClientWith(("Registration:Mode", "Closed"))
            .PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(NewEmail("closed"), "Password1", "Закрытый"));

        open.StatusCode.ShouldBe(HttpStatusCode.Created);
        closed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ProblemCodeAsync(closed)).ShouldBe("ApplicationUser.RegistrationClosed");
    }

    // ---------- коды приглашения ----------

    [DockerFact]
    public async Task InviteCode_ApprovesImmediately_RespectsLimit_AndCanBeDeactivated()
    {
        var client = ApprovalClient();
        var admin = await CreateAdminTokenAsync();
        var create = await SendJsonAsync(
            HttpMethod.Post, ApiRoutes.InviteCodes.List, new CreateInviteCodeRequest(null, "Группа ИС-21", null, 1), admin.AccessToken);
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var code = (await create.Content.ReadFromJsonAsync<InviteCodeDto>())!;
        code.Code.Length.ShouldBe(9);

        var withCode = await client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(NewEmail("coded"), "Password1", "С кодом", InviteCode: code.Code.ToLowerInvariant()));
        var exhausted = await client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(NewEmail("late"), "Password1", "Поздно", InviteCode: code.Code));

        withCode.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await withCode.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken.ShouldNotBeNullOrEmpty();
        exhausted.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var list = (await (await SendAsync(HttpMethod.Get, ApiRoutes.InviteCodes.List, admin.AccessToken))
            .Content.ReadFromJsonAsync<List<InviteCodeDto>>())!;
        list.Single(item => item.Id == code.Id).UsedCount.ShouldBe(1);

        var second = (await (await SendJsonAsync(
            HttpMethod.Post, ApiRoutes.InviteCodes.List, new CreateInviteCodeRequest(null, null, null, null), admin.AccessToken))
            .Content.ReadFromJsonAsync<InviteCodeDto>())!;
        (await SendAsync(HttpMethod.Post, $"{ApiRoutes.PrefixV1}/invite-codes/{second.Id}/deactivate", admin.AccessToken))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var deactivated = await client.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(NewEmail("off"), "Password1", "Выключен", InviteCode: second.Code));
        deactivated.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await Db.AuditEvents.AnyAsync(item => item.EventType == AuditEventTypes.InviteCodeCreated)).ShouldBeTrue();
        (await Db.AuditEvents.AnyAsync(item => item.EventType == AuditEventTypes.InviteCodeDeactivated)).ShouldBeTrue();
    }

    [DockerFact]
    public async Task InviteCode_Unknown_IsRejected_NoUserCreated()
    {
        var email = NewEmail("nocode");

        var response = await ApprovalClient().PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1", "Нет кода", InviteCode: "NO-SUCH-CODE"));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await UserManager.FindByEmailAsync(email)).ShouldBeNull();
    }

    [DockerFact]
    public async Task InviteCodes_RequireAdmin_DuplicateIs409_InvalidIs422()
    {
        var admin = await CreateAdminTokenAsync();
        var student = await RegisterOpenAsync(NewEmail("nonadmin"));
        var custom = "TEAM-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var asStudent = await SendAsync(HttpMethod.Get, ApiRoutes.InviteCodes.List, student.AccessToken);
        var created = await SendJsonAsync(HttpMethod.Post, ApiRoutes.InviteCodes.List, new CreateInviteCodeRequest(custom, null, null, null), admin.AccessToken);
        var duplicate = await SendJsonAsync(HttpMethod.Post, ApiRoutes.InviteCodes.List, new CreateInviteCodeRequest(custom.ToLowerInvariant(), null, null, null), admin.AccessToken);
        var invalid = await SendJsonAsync(HttpMethod.Post, ApiRoutes.InviteCodes.List, new CreateInviteCodeRequest("a b", null, null, 0), admin.AccessToken);

        asStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // ---------- личный кабинет ----------

    [DockerFact]
    public async Task Cabinet_ReadsProfile_ChangesName_WithAuditOfOldAndNewValues()
    {
        var me = await RegisterOpenAsync(NewEmail("cabinet"), "Старое Имя");

        var profile = (await (await SendAsync(HttpMethod.Get, ApiRoutes.Me.Profile, me.AccessToken))
            .Content.ReadFromJsonAsync<MyProfileDto>())!;
        profile.Email.ShouldBe(me.Email);
        profile.Roles.ShouldBe([RoleNames.Student]);
        profile.Status.ShouldBe(UserStatuses.Active);

        var update = await SendJsonAsync(
            HttpMethod.Put, ApiRoutes.Me.UpdateProfile, new UpdateUserProfileRequest("Анна", "Смирнова", "Игоревна"), me.AccessToken);

        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var changed = (await (await SendAsync(HttpMethod.Get, ApiRoutes.Me.Profile, me.AccessToken))
            .Content.ReadFromJsonAsync<MyProfileDto>())!;
        (changed.FirstName, changed.LastName, changed.MiddleName).ShouldBe(("Анна", "Смирнова", "Игоревна"));
        changed.DisplayName.ShouldBe("Анна Смирнова");
        var audit = await Db.AuditEvents.SingleAsync(item =>
            item.TargetUserId == me.UserId && item.EventType == AuditEventTypes.UserProfileSelfUpdated);
        audit.ActorUserId.ShouldBe(me.UserId);
        audit.Description.ShouldContain("Смирнова Анна Игоревна");
    }

    [DockerFact]
    public async Task Cabinet_RequiresAuthentication_AndValidatesName()
    {
        var me = await RegisterOpenAsync(NewEmail("cabinet2"));

        var anonymous = await HttpClient.GetAsync(ApiRoutes.Me.Profile);
        var empty = await SendJsonAsync(
            HttpMethod.Put, ApiRoutes.Me.UpdateProfile, new UpdateUserProfileRequest(" ", null, null), me.AccessToken);

        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        empty.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task Cabinet_ChangePassword_NeedsCurrent_RevokesSessions_WritesAudit()
    {
        var email = NewEmail("pass");
        var me = await RegisterOpenAsync(email);

        var wrong = await SendJsonAsync(
            HttpMethod.Put, ApiRoutes.Me.Password, new ChangeMyPasswordRequest("Wrong1234", "NewPass123"), me.AccessToken);
        var weak = await SendJsonAsync(
            HttpMethod.Put, ApiRoutes.Me.Password, new ChangeMyPasswordRequest("Password1", "short"), me.AccessToken);
        var ok = await SendJsonAsync(
            HttpMethod.Put, ApiRoutes.Me.Password, new ChangeMyPasswordRequest("Password1", "NewPass123"), me.AccessToken);

        wrong.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        weak.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ok.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1")))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "NewPass123")))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh, new RefreshRequest(me.RefreshToken)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var audit = await Db.AuditEvents.SingleAsync(item =>
            item.TargetUserId == me.UserId && item.EventType == AuditEventTypes.UserPasswordChanged);
        audit.Description.ShouldNotContain("NewPass123");
    }

    // ---------- защита от перебора ----------

    [DockerFact]
    public async Task Login_IsRateLimited_PerClient()
    {
        var client = ClientWith(("RateLimiting:AuthPermitLimit", "3"));
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            statuses.Add((await client.PostAsJsonAsync(
                ApiRoutes.Auth.Login, new LoginRequest("nobody@test.local", "Password1"))).StatusCode);
        }

        statuses.Take(3).ShouldAllBe(status => status == HttpStatusCode.Unauthorized);
        statuses.Skip(3).ShouldAllBe(status => status == HttpStatusCode.TooManyRequests);
    }

    // ---------- helpers ----------

    private static string NewEmail(string prefix) => $"{prefix}_{Guid.NewGuid():N}@test.local";

    private async Task<TokenResponse> RegisterOpenAsync(string email, string? displayName = "Test User")
    {
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1", displayName));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private async Task<ApplicationUser> FindUserAsync(string email)
    {
        var user = await UserManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        return user;
    }

    private async Task<TokenResponse> CreateAdminTokenAsync()
    {
        var email = NewEmail("admin");
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

    private async Task<UserDetailsDto> GetDetailsAsync(Guid userId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"{ApiRoutes.PrefixV1}/users/{userId}", token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDetailsDto>())!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await HttpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string path, T body, string token)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return await HttpClient.SendAsync(request);
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static string ApprovePath(Guid id) => $"{ApiRoutes.PrefixV1}/users/{id}/approve";

    private static string RejectPath(Guid id) => $"{ApiRoutes.PrefixV1}/users/{id}/reject";
}
