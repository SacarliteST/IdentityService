namespace IdentityService.Contracts;

/// <summary>Событие безопасности или административное действие.</summary>
/// <param name="Id">Идентификатор события.</param>
/// <param name="ActorUserId">Кто выполнил действие; <c>null</c> — система.</param>
/// <param name="TargetUserId">Над кем выполнено действие.</param>
/// <param name="EventType">Тип события.</param>
/// <param name="Description">Описание.</param>
/// <param name="CreatedAt">Время события.</param>
/// <param name="ActorName">ФИО инициатора (или отображаемое имя); <c>null</c>, если инициатора нет или он удалён.</param>
/// <param name="ActorEmail">Email инициатора.</param>
/// <param name="TargetName">ФИО объекта (или отображаемое имя).</param>
/// <param name="TargetEmail">Email объекта.</param>
public sealed record AuditEventDto(
    Guid Id,
    Guid? ActorUserId,
    Guid? TargetUserId,
    string EventType,
    string Description,
    DateTimeOffset CreatedAt,
    string? ActorName = null,
    string? ActorEmail = null,
    string? TargetName = null,
    string? TargetEmail = null);
