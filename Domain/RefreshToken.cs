namespace IdentityService.Domain;

/// <summary>
/// Доменная сущность refresh-токена. Инкапсулирует логику жизненного цикла:
/// создание через фабричный метод, проверку активности и отзыв.
/// В БД хранится только хэш токена — сырое значение нигде не персистируется.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>Суррогатный PK.</summary>
    public Guid Id { get; private set; }

    /// <summary>FK на <see cref="ApplicationUser"/>.</summary>
    public Guid UserId { get; private set; }

    /// <summary>SHA-256 хэш сырого refresh-токена.</summary>
    public string TokenHash { get; private set; } = String.Empty;

    /// <summary>Момент истечения срока действия.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Момент создания.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Момент отзыва. <c>null</c> — токен ещё не отозван.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Хэш токена, который заменил текущий при ротации. Используется для аудита цепочки ротации.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Приватный конструктор для EF Core.</summary>
    private RefreshToken() { }

    /// <summary>Создаёт новый токен. Единственная точка входа — гарантирует заполнение всех обязательных полей.</summary>
    public static RefreshToken Create(
        Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt
        };

    /// <summary>Возвращает <c>true</c>, если токен не отозван и не истёк на момент <paramref name="now"/>.</summary>
    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && ExpiresAt > now;

    /// <summary>
    /// Отзывает токен. При ротации передайте <paramref name="replacedByTokenHash"/>
    /// для сохранения цепочки замен.
    /// </summary>
    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
