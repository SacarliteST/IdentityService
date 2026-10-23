using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.RejectUser;

internal sealed class RejectUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Reject, Handle)
            .WithName("RejectUser")
            .WithTags("Пользователи")
            .WithSummary("Отклонение заявки на регистрацию")
            .WithDescription(
                "Отклоняет заявку в ожидании. Уже одобренного пользователя отклонить нельзя (409) — используйте блокировку. " +
                "Отклонённую заявку можно позже одобрить. Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<RejectUserRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        RejectUserRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<RejectUserCommand, Result>(
            new RejectUserCommand(actorUserId, id, request.Reason), ct);
        return result.ToNoContent();
    }
}
