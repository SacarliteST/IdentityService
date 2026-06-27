using System.Net;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.IntegrationTests.Infrastructure;
using Shouldly;

namespace IdentityService.IntegrationTests.Auth;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RefreshLogoutTests(TestApplication app) : ApiTestBase(app)
{
    private async Task<TokenResponse> RegisterAndLoginAsync()
    {
        var email = $"refresh_{Guid.NewGuid()}@test.com";
        var reg = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", null));
        return (await reg.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    [DockerFact]
    public async Task Refresh_ValidToken_ReturnsNewPair()
    {
        var original = await RegisterAndLoginAsync();

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh,
            new RefreshRequest(original.RefreshToken));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();
        newTokens.ShouldNotBeNull();
        newTokens.RefreshToken.ShouldNotBe(original.RefreshToken);
        newTokens.AccessToken.ShouldNotBe(original.AccessToken);
    }

    [DockerFact]
    public async Task Refresh_OldTokenAfterRotation_Returns401_ReuseDetection()
    {
        var original = await RegisterAndLoginAsync();

        // First rotation succeeds
        await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh,
            new RefreshRequest(original.RefreshToken));

        // Reuse of the already-rotated (revoked) token → reuse-detection
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh,
            new RefreshRequest(original.RefreshToken));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Logout_ThenRefresh_Returns401()
    {
        var tokens = await RegisterAndLoginAsync();

        var logout = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Logout,
            new RefreshRequest(tokens.RefreshToken));
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refresh = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh,
            new RefreshRequest(tokens.RefreshToken));
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DockerFact]
    public async Task Logout_Twice_Returns204_Idempotent()
    {
        var tokens = await RegisterAndLoginAsync();

        var first = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Logout,
            new RefreshRequest(tokens.RefreshToken));
        var second = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Logout,
            new RefreshRequest(tokens.RefreshToken));

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
