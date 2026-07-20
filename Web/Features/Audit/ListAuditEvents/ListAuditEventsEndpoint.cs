using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Audit.ListAuditEvents;

internal sealed class ListAuditEventsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Audit.List, Handle)
            .WithName("ListAuditEvents")
            .WithTags("Аудит")
            .WithSummary("Журнал аудита")
            .WithDescription("Возвращает постраничный журнал событий. Доступно только для роли Admin.")
            .Produces<PagedResponse<AuditEventDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAuditEventsRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAuditEventsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            ListAuditEventsQuery,
            Result<PagedResponse<AuditEventDto>>>(
            new ListAuditEventsQuery(
                request.Page ?? 1,
                request.PageSize ?? 20,
                request.ActorUserId,
                request.TargetUserId,
                request.EventType,
                request.From,
                request.To),
            ct);

        return result.ToOk();
    }
}
