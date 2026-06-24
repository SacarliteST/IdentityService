using IdentityService.Host.Common;
using IdentityService.Host.Common.Options;
using Microsoft.Extensions.Options;

namespace IdentityService.Host.Features.Discovery;

internal sealed class OpenIdConfigurationEndpoint(IOptions<JwtOptions> jwtOptions) : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/openid-configuration", Handle)
            .WithName("OpenIdConfiguration")
            .WithTags("Discovery")
            .WithSummary("OpenID Connect discovery document")
            .WithDescription("Minimal OIDC discovery document. Set Authority = Issuer in client JwtBearer options.")
            .Produces<object>(StatusCodes.Status200OK, "application/json")
            .AllowAnonymous();
    }

    private IResult Handle()
    {
        var issuer = jwtOptions.Value.Issuer;
        var response = new
        {
            issuer,
            jwks_uri = $"{issuer}/.well-known/jwks.json",
            id_token_signing_alg_values_supported = new[] { "RS256" },
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" }
        };
        return Results.Json(response, contentType: "application/json");
    }
}
