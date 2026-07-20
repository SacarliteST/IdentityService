using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.UnblockUser;

internal sealed class UnblockUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Unblock, Handle)
            .WithName("UnblockUser")
            .WithTags("Пользователи")
            .WithSummary("Разблокировка пользователя")
            .WithDescription("Снимает блокировку и очищает её административные метаданные. Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<UnblockUserCommand, Result>(
            new UnblockUserCommand(
                Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
                id),
            ct);

        return result.ToNoContent();
    }
}
