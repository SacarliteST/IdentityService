using IdentityService.Contracts;
using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Audit;

internal static class AuditEventQuery
{
    internal static IQueryable<AuditEvent> ApplyFilters(
        IQueryable<AuditEvent> query,
        Guid? actorUserId,
        Guid? targetUserId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (actorUserId.HasValue)
        {
            query = query.Where(auditEvent => auditEvent.ActorUserId == actorUserId);
        }

        if (targetUserId.HasValue)
        {
            query = query.Where(auditEvent => auditEvent.TargetUserId == targetUserId);
        }

        if (!String.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(auditEvent => auditEvent.EventType == eventType);
        }

        if (from.HasValue)
        {
            query = query.Where(auditEvent => auditEvent.CreatedAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(auditEvent => auditEvent.CreatedAt <= to);
        }

        return query;
    }

    internal static async Task<PagedResponse<AuditEventDto>> ToPageAsync(
        IQueryable<AuditEvent> query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(auditEvent => new AuditEventDto(
                auditEvent.Id,
                auditEvent.ActorUserId,
                auditEvent.TargetUserId,
                auditEvent.EventType,
                auditEvent.Description,
                auditEvent.CreatedAt))
            .ToListAsync(ct);

        return new PagedResponse<AuditEventDto>(items, page, pageSize, totalCount);
    }
}
