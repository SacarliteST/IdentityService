using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Audit.ListAuditEvents;

internal sealed record ListAuditEventsQuery(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    Guid? TargetUserId,
    string? EventType,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<Result<PagedResponse<AuditEventDto>>>;
