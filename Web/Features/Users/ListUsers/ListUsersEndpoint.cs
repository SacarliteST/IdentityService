using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.ListUsers;

internal sealed class ListUsersEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Users.List, Handle)
            .WithName("ListUsers")
            .WithTags("Пользователи")
            .WithSummary("Список пользователей")
            .WithDescription("Возвращает постраничный список пользователей. Доступно только для роли Admin.")
            .Produces<PagedResponse<UserListItemDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetUsersRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetUsersRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<ListUsersQuery, Result<PagedResponse<UserListItemDto>>>(
            new ListUsersQuery(
                request.Page ?? 1,
                request.PageSize ?? 20,
                request.Search,
                request.Role,
                request.Status),
            ct);

        return result.ToOk();
    }
}
