namespace IdentityService.Contracts;

/// <summary>Фильтры общего журнала аудита.</summary>
public sealed class GetAuditEventsRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public Guid? ActorUserId { get; init; }

    public Guid? TargetUserId { get; init; }

    public string? EventType { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
