namespace IdentityService.Domain;

/// <summary>
/// Код приглашения: при самостоятельной регистрации с действующим кодом заявка одобряется сразу, без ожидания администратора.
/// </summary>
public sealed class InviteCode
{
    public Guid Id { get; private set; }

    /// <summary>Код в нормализованном виде (верхний регистр, без пробелов по краям).</summary>
    public string Code { get; private set; } = String.Empty;

    public string? Note { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Сколько раз можно использовать; <see langword="null"/> — без ограничения.</summary>
    public int? MaxUses { get; private set; }

    public int UsedCount { get; private set; }

    public bool IsActive { get; private set; } = true;

    private InviteCode()
    {
    }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static InviteCode Create(
        string code,
        string? note,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt,
        int? maxUses) =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = Normalize(code),
            Note = PersonName.Normalize(note),
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            MaxUses = maxUses,
        };

    /// <summary>Код можно применить сейчас: включён, не истёк и не исчерпан.</summary>
    public bool IsUsable(DateTimeOffset now) =>
        IsActive && (ExpiresAt is null || ExpiresAt > now) && (MaxUses is null || UsedCount < MaxUses);

    public void Deactivate() => IsActive = false;
}
