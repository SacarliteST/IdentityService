using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Auth.Logout;

internal sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Logout, Handle)
            .WithName("Logout")
            .WithTags("Аутентификация")
            .WithSummary("Выход из системы")
            .WithDescription("Отзывает refresh-токен. Идемпотентно: повторный вызов возвращает 204.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<RefreshRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        RefreshRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<LogoutCommand, Result>(
            new LogoutCommand(request.RefreshToken), ct);

        return result.ToNoContent();
    }
}
