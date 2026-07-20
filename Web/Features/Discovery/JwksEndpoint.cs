using IdentityService.Web.Common;
using IdentityService.Web.Common.Keys;

namespace IdentityService.Web.Features.Discovery;

internal sealed class JwksEndpoint(ISigningKeyProvider signingKeyProvider) : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/jwks.json", Handle)
            .WithName("Jwks")
            .WithTags("Метаданные")
            .WithSummary("Набор открытых ключей JWKS")
            .WithDescription("Возвращает открытый RSA-ключ для проверки подписи JWT. Issuer должен совпадать с Authority клиентского сервиса.")
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
