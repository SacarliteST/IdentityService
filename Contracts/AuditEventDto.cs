namespace IdentityService.Contracts;

/// <summary>Событие безопасности или административное действие.</summary>
public sealed record AuditEventDto(
    Guid Id,
    Guid? ActorUserId,
    Guid? TargetUserId,
    string EventType,
    string Description,
    DateTimeOffset CreatedAt);
