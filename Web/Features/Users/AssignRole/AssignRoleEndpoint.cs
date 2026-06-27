using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.AssignRole;

internal sealed class AssignRoleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Roles, Handle)
            .WithName("AssignRole")
            .WithTags("Users")
            .WithSummary("Присвоение роли пользователю")
            .WithDescription("Только для Admin. Присваивает существующую роль указанному пользователю.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<AssignRoleRequest>>()
            .RequireAuthorization(p => p.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id, AssignRoleRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<AssignRoleCommand, Result>(
            new AssignRoleCommand(id, request.Role), ct);

        return result.ToNoContent();
    }
}
