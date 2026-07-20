using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.UpdateUserRoles;

internal sealed class UpdateUserRolesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Users.Roles, Handle)
            .WithName("UpdateUserRoles")
            .WithTags("Пользователи")
            .WithSummary("Замена ролей пользователя")
            .WithDescription("Атомарно заменяет полный набор ролей. Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<UpdateUserRolesRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateUserRolesRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<UpdateUserRolesCommand, Result>(
            new UpdateUserRolesCommand(actorUserId, id, request.Roles),
            ct);

        return result.ToNoContent();
    }
}
