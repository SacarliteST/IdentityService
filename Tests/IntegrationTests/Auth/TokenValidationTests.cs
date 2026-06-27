using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using IdentityService.Contracts;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace IdentityService.IntegrationTests.Auth;

/// <summary>
/// E5: Валидация выпущенного access-токена по публичному JWK из /.well-known/jwks.json.
/// Доказывает совместимость подписи и параметров с тем, что модули будут использовать.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class TokenValidationTests(TestApplication app) : ApiTestBase(app)
{
    [DockerFact]
    public async Task AccessToken_ValidatesSuccessfully_UsingPublicJwk()
    {
        // 1. Register + Login to get a token
        var email = $"val_{Guid.NewGuid()}@test.com";
        var reg = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest(email, "Password1!", "Val User"));
        var tokens = (await reg.Content.ReadFromJsonAsync<TokenResponse>())!;

        // 2. Fetch public JWKS
        var jwksJson = await HttpClient.GetStringAsync("/.well-known/jwks.json");
        var jwksDoc = JsonDocument.Parse(jwksJson);
        var keyEl = jwksDoc.RootElement.GetProperty("keys")[0];

        var jwk = new JsonWebKey
        {
            Kty = keyEl.GetProperty("kty").GetString(),
            Use = keyEl.GetProperty("use").GetString(),
            Alg = keyEl.GetProperty("alg").GetString(),
            Kid = keyEl.GetProperty("kid").GetString(),
            N = keyEl.GetProperty("n").GetString(),
            E = keyEl.GetProperty("e").GetString()
        };

        // 3. Validate the access token with parameters a module would use
        var tvp = new TokenValidationParameters
        {
            IssuerSigningKey = jwk,
            ValidIssuer = TestApplication.TestIssuer,
            ValidAudience = TestApplication.TestAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(tokens.AccessToken, tvp, out _);

        // 4. Verify claims
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        userId.ShouldBe(tokens.UserId.ToString());

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.ShouldContain(RoleNames.Student);
    }

    [DockerFact]
    public async Task AccessToken_KidMatchesJwks()
    {
        var reg = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register,
            new RegisterRequest($"kid_{Guid.NewGuid()}@test.com", "Password1!", null));
        var tokens = (await reg.Content.ReadFromJsonAsync<TokenResponse>())!;

        var jwksJson = await HttpClient.GetStringAsync("/.well-known/jwks.json");
        var jwksDoc = JsonDocument.Parse(jwksJson);
        var jwkKid = jwksDoc.RootElement.GetProperty("keys")[0].GetProperty("kid").GetString();

        // Decode JWT header to get kid
        var headerB64 = tokens.AccessToken.Split('.')[0];
        var headerPadded = headerB64.Length % 4 == 0
            ? headerB64
            : headerB64 + new string('=', 4 - headerB64.Length % 4);
        var headerJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(
            headerPadded.Replace('-', '+').Replace('_', '/')));
        var headerDoc = JsonDocument.Parse(headerJson);
        var tokenKid = headerDoc.RootElement.GetProperty("kid").GetString();

        tokenKid.ShouldBe(jwkKid);
    }
}
