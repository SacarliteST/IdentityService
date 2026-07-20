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
            .WithTags("Users")
            .WithSummary("Разблокировка пользователя")
            .WithDescription("Снимает блокировку и очищает её административные метаданные. Только для Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<UnblockUserCommand, Result>(
            new UnblockUserCommand(id),
            ct);

        return result.ToNoContent();
    }
}
