namespace IdentityService.Domain;

/// <summary>
/// Учебная группа — справочник, из которого выбирается группа для кода приглашения.
/// Название уникально; удаление возможно, только когда группа нигде не используется.
/// </summary>
public sealed class Group
{
    /// <summary>Максимальная длина названия группы (как в учебном профиле Education).</summary>
    public const int NameMaxLength = 50;

    public Guid Id { get; private set; }

    /// <summary>Название в нормализованном виде (без пробелов по краям).</summary>
    public string Name { get; private set; } = String.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Коды приглашения, ведущие в эту группу.</summary>
    public List<InviteCode> InviteCodes { get; private set; } = [];

    /// <summary>Пользователи, зачисленные в эту группу.</summary>
    public List<ApplicationUser> Users { get; private set; } = [];

    private Group()
    {
    }

    /// <summary>Приводит название группы к каноническому виду: без пробелов по краям, пустое — <see langword="null"/>.</summary>
    public static string? NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        return String.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static Group Create(string name, DateTimeOffset createdAt) =>
        new() { Id = Guid.NewGuid(), Name = NormalizeName(name)!, CreatedAt = createdAt };

    public void Rename(string name) => Name = NormalizeName(name)!;
}
