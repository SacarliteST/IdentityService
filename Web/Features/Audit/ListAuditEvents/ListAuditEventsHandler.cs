using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Audit.ListAuditEvents;

internal sealed class ListAuditEventsHandler(AppDbContext db)
    : IRequestHandler<ListAuditEventsQuery, Result<PagedResponse<AuditEventDto>>>
{
    public async Task<Result<PagedResponse<AuditEventDto>>> Handle(
        ListAuditEventsQuery query,
        CancellationToken ct)
    {
        var auditEvents = AuditEventQuery.ApplyFilters(
            db.AuditEvents.AsNoTracking(),
            query.ActorUserId,
            query.TargetUserId,
            query.EventType,
            query.From,
            query.To);

        return await AuditEventQuery.ToPageAsync(
            auditEvents,
            query.Page,
            query.PageSize,
            ct);
    }
}
