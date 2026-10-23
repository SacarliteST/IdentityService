using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.ApproveUser;

internal sealed class ApproveUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Approve, Handle)
            .WithName("ApproveUser")
            .WithTags("Пользователи")
            .WithSummary("Одобрение заявки на регистрацию")
            .WithDescription("Открывает пользователю доступ. Повторный вызов безопасен. Доступно только для роли Admin.")
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
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<ApproveUserCommand, Result>(new ApproveUserCommand(actorUserId, id), ct);
        return result.ToNoContent();
    }
}
