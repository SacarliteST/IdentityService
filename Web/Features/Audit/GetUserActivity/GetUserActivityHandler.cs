using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Web.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Audit.GetUserActivity;

internal sealed class GetUserActivityHandler(AppDbContext db)
    : IRequestHandler<GetUserActivityQuery, Result<PagedResponse<AuditEventDto>>>
{
    public async Task<Result<PagedResponse<AuditEventDto>>> Handle(
        GetUserActivityQuery query,
        CancellationToken ct)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(user => user.Id == query.UserId, ct))
        {
            return Result<PagedResponse<AuditEventDto>>.Fail(AuthErrors.UserNotFound(query.UserId));
        }

        var auditEvents = db.AuditEvents
            .AsNoTracking()
            .Where(auditEvent =>
                auditEvent.ActorUserId == query.UserId ||
                auditEvent.TargetUserId == query.UserId);
        auditEvents = AuditEventQuery.ApplyFilters(
            auditEvents,
            null,
            null,
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
