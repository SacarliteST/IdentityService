using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.BlockUser;

internal sealed class BlockUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Block, Handle)
            .WithName("BlockUser")
            .WithTags("Пользователи")
            .WithSummary("Блокировка пользователя")
            .WithDescription("Блокирует вход и отзывает активные refresh-токены. Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<BlockUserRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        BlockUserRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<BlockUserCommand, Result>(
            new BlockUserCommand(actorUserId, id, request.Reason),
            ct);

        return result.ToNoContent();
    }
}
