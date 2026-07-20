using IdentityService.Contracts;

namespace IdentityService.Web.Common;

internal static class OpenApiRouteExtensions
{
    internal static RouteHandlerBuilder ProducesValidationProblem(this RouteHandlerBuilder builder) =>
        builder.Produces<ValidationProblemDetails>(
            StatusCodes.Status422UnprocessableEntity,
            "application/problem+json");
}
