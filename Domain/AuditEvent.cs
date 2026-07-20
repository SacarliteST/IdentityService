namespace IdentityService.Domain;

public sealed class AuditEvent
{
    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? TargetUserId { get; private set; }

    public string EventType { get; private set; } = String.Empty;

    public string Description { get; private set; } = String.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private AuditEvent() { }

    public static AuditEvent Create(
        Guid? actorUserId,
        Guid? targetUserId,
        string eventType,
        string description,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            EventType = eventType,
            Description = description,
            CreatedAt = createdAt
        };
}
