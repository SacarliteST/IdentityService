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

    // users — пользователи для подстановки ФИО и email инициатора и объекта события.
    internal static async Task<PagedResponse<AuditEventDto>> ToPageAsync(
        IQueryable<AuditEvent> query,
        IQueryable<ApplicationUser> users,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(auditEvent => new
            {
                Event = auditEvent,
                Actor = users
                    .Where(user => user.Id == auditEvent.ActorUserId)
                    .Select(user => new { user.Email, user.LastName, user.FirstName, user.MiddleName, user.DisplayName })
                    .FirstOrDefault(),
                Target = users
                    .Where(user => user.Id == auditEvent.TargetUserId)
                    .Select(user => new { user.Email, user.LastName, user.FirstName, user.MiddleName, user.DisplayName })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(row => new AuditEventDto(
                row.Event.Id,
                row.Event.ActorUserId,
                row.Event.TargetUserId,
                row.Event.EventType,
                row.Event.Description,
                row.Event.CreatedAt,
                row.Actor is null ? null : PersonName.BuildFullName(row.Actor.LastName, row.Actor.FirstName, row.Actor.MiddleName) ?? row.Actor.DisplayName,
                row.Actor?.Email,
                row.Target is null ? null : PersonName.BuildFullName(row.Target.LastName, row.Target.FirstName, row.Target.MiddleName) ?? row.Target.DisplayName,
                row.Target?.Email))
            .ToList();

        return new PagedResponse<AuditEventDto>(items, page, pageSize, totalCount);
    }
}
