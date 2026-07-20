using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Audit.GetUserActivity;

internal sealed record GetUserActivityQuery(
    Guid UserId,
    int Page,
    int PageSize,
    string? EventType,
    DateTimeOffset? From,
    DateTimeOffset? To) : IRequest<Result<PagedResponse<AuditEventDto>>>;
