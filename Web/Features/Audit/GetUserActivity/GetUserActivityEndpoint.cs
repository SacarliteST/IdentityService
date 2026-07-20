using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Audit.GetUserActivity;

internal sealed class GetUserActivityEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Users.Activity, Handle)
            .WithName("GetUserActivity")
            .WithTags("Аудит")
            .WithSummary("Активность пользователя")
            .WithDescription("Возвращает события пользователя как инициатора или объекта действия. Доступно только для роли Admin.")
            .Produces<PagedResponse<AuditEventDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetUserActivityRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        [AsParameters] GetUserActivityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            GetUserActivityQuery,
            Result<PagedResponse<AuditEventDto>>>(
            new GetUserActivityQuery(
                id,
                request.Page ?? 1,
                request.PageSize ?? 20,
                request.EventType,
                request.From,
                request.To),
            ct);

        return result.ToOk();
    }
}
