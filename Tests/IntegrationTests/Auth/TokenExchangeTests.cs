using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace IdentityService.IntegrationTests.Auth;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TokenExchangeTests(TestApplication app) : ApiTestBase(app)
{
    private static readonly JsonWebTokenHandler JwtReader = new();

    [DockerFact]
    public async Task Exchange_ValidClientAndSubjectToken_Returns200WithScopedToken()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
        body.ExpiresIn.ShouldBeGreaterThan(0);

        var exchanged = JwtReader.ReadJsonWebToken(body.AccessToken);
        exchanged.Audiences.ShouldContain(TestApplication.TestExchangeAudience);
        exchanged.Audiences.ShouldNotContain(TestApplication.TestAudience);
        exchanged.TryGetClaim("session_id", out _).ShouldBeFalse();
    }

    [DockerFact]
    public async Task Exchange_WithoutSessionId_TokenCarriesSubjectRoles()
    {
        // TAH-I1: handoff преподавателя в SQL-модуль держится на том, что роль
        // субъекта переживает обмен, а claim session_id не появляется.
        var subjectToken = await IssueSubjectTokenAsync(RoleNames.Teacher);

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        var exchanged = JwtReader.ReadJsonWebToken(body!.AccessToken);

        exchanged.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ShouldContain(RoleNames.Teacher);
        exchanged.TryGetClaim("session_id", out _).ShouldBeFalse();
        exchanged.Audiences.ShouldContain(TestApplication.TestExchangeAudience);
    }

    [DockerFact]
    public async Task Exchange_WithSessionId_TokenCarriesSessionIdClaim()
    {
        var subjectToken = await IssueSubjectTokenAsync();
        var sessionId = Guid.NewGuid().ToString();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience, sessionId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        var exchanged = JwtReader.ReadJsonWebToken(body!.AccessToken);
        exchanged.GetClaim("session_id").Value.ShouldBe(sessionId);
        exchanged.Audiences.ShouldContain(TestApplication.TestExchangeAudience);
    }

    [DockerFact]
    public async Task Exchange_SessionExpiresBeforeDefaultTtl_ClampsExpiresIn()
    {
        var subjectToken = await IssueSubjectTokenAsync();
        var sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3);

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience,
            Guid.NewGuid().ToString(), sessionExpiresAt);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        // стандартный TTL обмена — 30 мин; сессия истекает через 3 → exp обрезан
        body!.ExpiresIn.ShouldBeLessThanOrEqualTo(3 * 60);
        body.ExpiresIn.ShouldBeGreaterThan(0);
    }

    [DockerFact]
    public async Task Exchange_SessionLongerThanDefaultTtl_TokenLivesUntilSessionEnd()
    {
        var subjectToken = await IssueSubjectTokenAsync();
        var sessionExpiresAt = DateTimeOffset.UtcNow.AddHours(2);

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience,
            Guid.NewGuid().ToString(), sessionExpiresAt);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        // стандартный TTL обмена — 30 мин, но токен практической сессии живёт до её конца
        body!.ExpiresIn.ShouldBeInRange(2 * 3600 - 60, 2 * 3600);
    }

    [DockerFact]
    public async Task Exchange_SessionLongerThanMax_TokenCappedAtSessionTokenMaxHours()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience,
            Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.AddHours(20));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        body!.ExpiresIn.ShouldBeInRange(8 * 3600 - 60, 8 * 3600);
    }

    [DockerFact]
    public async Task Exchange_SessionWithoutTimeLimit_TokenLivesSessionTokenMaxHours()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience,
            Guid.NewGuid().ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        body!.ExpiresIn.ShouldBeInRange(8 * 3600 - 60, 8 * 3600);
    }

    [DockerFact]
    public async Task Exchange_WithoutSessionId_KeepsDefaultTtl()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();
        body!.ExpiresIn.ShouldBeInRange(30 * 60 - 60, 30 * 60);
    }

    [DockerFact]
    public async Task Exchange_WrongClientSecret_Returns401()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, "wrong-secret",
            subjectToken, TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Exchange_UnknownClient_Returns401()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            "unknown-client", "whatever",
            subjectToken, TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Exchange_AudienceNotInAllowList_Returns401()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            subjectToken, "some-other-service-api");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Exchange_InvalidSubjectToken_Returns401()
    {
        var response = await SendExchangeRequestAsync(
            TestApplication.TestClientId, TestApplication.TestClientSecret,
            "not-a-real-token", TestApplication.TestExchangeAudience);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Exchange_MissingAuthorizationHeader_Returns401()
    {
        var subjectToken = await IssueSubjectTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Auth.TokenExchange)
        {
            Content = JsonContent.Create(new TokenExchangeRequest(
                "urn:ietf:params:oauth:grant-type:token-exchange",
                subjectToken,
                TestApplication.TestExchangeAudience))
        };

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string> IssueSubjectTokenAsync(params string[] roles)
    {
        var email = $"exchange_{Guid.NewGuid()}@test.com";
        const string password = "Password1!";

        await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, password, null));

        if (roles.Length > 0)
        {
            var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email)
                ?? throw new InvalidOperationException($"Зарегистрированный пользователь {email} не найден.");
            (await userManager.AddToRolesAsync(user, roles)).Succeeded.ShouldBeTrue();
        }

        var loginResponse = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Login,
            new LoginRequest(email, password));

        var tokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendExchangeRequestAsync(
        string clientId, string clientSecret, string subjectToken, string audience,
        string? sessionId = null, DateTimeOffset? sessionExpiresAt = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Auth.TokenExchange)
        {
            Content = JsonContent.Create(new TokenExchangeRequest(
                "urn:ietf:params:oauth:grant-type:token-exchange", subjectToken, audience,
                sessionId, sessionExpiresAt))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        return await HttpClient.SendAsync(request);
    }
}
