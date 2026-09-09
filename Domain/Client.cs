namespace IdentityService.Domain;

/// <summary>
/// Доверенный сервисный клиент, которому разрешён Token Exchange — обмен токена
/// пользователя, выданного этим инстансом, на новый токен, ограниченный конкретной
/// целевой <c>audience</c> (например, API внешнего практического модуля).
/// Секрет клиента в БД не хранится — только его хэш.
/// </summary>
public sealed class Client
{
    /// <summary>Суррогатный PK.</summary>
    public Guid Id { get; private set; }

    /// <summary>Публичный идентификатор клиента — логин в Basic-аутентификации эндпоинта обмена.</summary>
    public string ClientId { get; private set; } = String.Empty;

    /// <summary>SHA-256 хэш секрета клиента.</summary>
    public string ClientSecretHash { get; private set; } = String.Empty;

    /// <summary>
    /// Аудитории, которые клиенту разрешено запрашивать через обмен. Явный allow-list —
    /// клиент не может запросить произвольную строку.
    /// </summary>
    public string[] AllowedAudiences { get; private set; } = [];

    /// <summary>Отключённый клиент не проходит аутентификацию при обмене.</summary>
    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Приватный конструктор для EF Core.</summary>
    private Client() { }

    /// <summary>Создаёт нового клиента. Единственная точка входа — гарантирует заполнение всех обязательных полей.</summary>
    public static Client Create(
        string clientId,
        string clientSecretHash,
        IReadOnlyList<string> allowedAudiences,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            AllowedAudiences = allowedAudiences.ToArray(),
            IsEnabled = true,
            CreatedAt = createdAt
        };

    /// <summary>Возвращает <c>true</c>, если клиент активен и вправе запросить указанную аудиторию.</summary>
    public bool CanRequestAudience(string audience) =>
        IsEnabled && AllowedAudiences.Contains(audience, StringComparer.Ordinal);
}
