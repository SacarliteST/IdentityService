using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Health;

internal sealed class HealthEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => TypedResults.Ok())
            .WithName("Health")
            .WithTags("Состояние")
            .Produces(StatusCodes.Status200OK);
    }
}
