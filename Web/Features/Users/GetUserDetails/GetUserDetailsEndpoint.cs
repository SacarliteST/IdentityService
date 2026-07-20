using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.GetUserDetails;

internal sealed class GetUserDetailsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Users.ById, Handle)
            .WithName("GetUserDetails")
            .WithTags("Пользователи")
            .WithSummary("Карточка пользователя")
            .WithDescription("Возвращает пользователя и его роли. Доступно только для роли Admin.")
            .Produces<UserDetailsDto>()
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
        var result = await sender.Send<GetUserDetailsQuery, Result<UserDetailsDto>>(
            new GetUserDetailsQuery(id),
            ct);

        return result.ToOk();
    }
}
