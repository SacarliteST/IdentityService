using System.Net;
using System.Text.Json;
using IdentityService.IntegrationTests.Infrastructure;
using Shouldly;

namespace IdentityService.IntegrationTests.Discovery;

public sealed class JwksEndpointTests(TestApplication testApplication) : ApiTestBase(testApplication)
{
    [DockerFact]
    public async Task Jwks_Returns200_WithSingleRsaKey()
    {
        var response = await HttpClient.GetAsync("/.well-known/jwks.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var keys = doc.RootElement.GetProperty("keys");
        keys.GetArrayLength().ShouldBe(1);

        var key = keys[0];
        key.GetProperty("kty").GetString().ShouldBe("RSA");
        key.GetProperty("use").GetString().ShouldBe("sig");
        key.GetProperty("alg").GetString().ShouldBe("RS256");
        key.TryGetProperty("kid", out var kidProp).ShouldBeTrue();
        kidProp.GetString().ShouldNotBeNullOrEmpty();
        key.TryGetProperty("n", out var nProp).ShouldBeTrue();
        nProp.GetString().ShouldNotBeNullOrEmpty();
        key.TryGetProperty("e", out var eProp).ShouldBeTrue();
        eProp.GetString().ShouldNotBeNullOrEmpty();

        // Private key components must not be present
        key.TryGetProperty("d", out _).ShouldBeFalse();
        key.TryGetProperty("p", out _).ShouldBeFalse();
        key.TryGetProperty("q", out _).ShouldBeFalse();
        key.TryGetProperty("dp", out _).ShouldBeFalse();
        key.TryGetProperty("dq", out _).ShouldBeFalse();
        key.TryGetProperty("qi", out _).ShouldBeFalse();
    }

    [DockerFact]
    public async Task Discovery_Returns200_WithValidDocument()
    {
        var response = await HttpClient.GetAsync("/.well-known/openid-configuration");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        doc.RootElement.TryGetProperty("issuer", out _).ShouldBeTrue();

        doc.RootElement.TryGetProperty("jwks_uri", out var jwksUri).ShouldBeTrue();
        (jwksUri.GetString() ?? string.Empty).ShouldContain("/.well-known/jwks.json");

        doc.RootElement.TryGetProperty("id_token_signing_alg_values_supported", out var algs).ShouldBeTrue();
        algs.EnumerateArray().Select(a => a.GetString()).ShouldContain("RS256");
    }
}
