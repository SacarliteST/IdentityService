using IdentityService.Web.Common;
using IdentityService.Web.Common.Keys;

namespace IdentityService.Web.Features.Discovery;

internal sealed class JwksEndpoint(ISigningKeyProvider signingKeyProvider) : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/jwks.json", Handle)
            .WithName("Jwks")
            .WithTags("Discovery")
            .WithSummary("JSON Web Key Set")
            .WithDescription("Returns the public RSA key used to verify JWT signatures. Issuer must match Authority configured in client services.")
            .Produces<object>(StatusCodes.Status200OK, "application/json")
            .AllowAnonymous();
    }

    private IResult Handle()
    {
        var jwk = signingKeyProvider.GetPublicJwk();
        var response = new
        {
            keys = new[]
            {
                new
                {
                    kty = jwk.Kty,
                    use = jwk.Use,
                    alg = jwk.Alg,
                    kid = jwk.Kid,
                    n = jwk.N,
                    e = jwk.E
                }
            }
        };
        return Results.Json(response, contentType: "application/json");
    }
}
