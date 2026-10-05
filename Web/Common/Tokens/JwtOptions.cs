namespace IdentityService.Web.Common.Tokens;

public sealed class JwtOptions
{
    public const string SectionKey = "Jwt";

    public string Issuer { get; init; } = String.Empty;
    public string Audience { get; init; } = String.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;

    /// <summary>
    /// TTL токена, выданного через Token Exchange (<c>POST /auth/token/exchange</c>) под
    /// audience другого сервиса. Может быть длиннее обычного access-токена — аудитория узкая,
    /// риск от увеличения TTL ниже, чем у токена, годного против всех сервисов платформы.
    /// </summary>
    public int ExchangeAccessTokenMinutes { get; init; } = 30;

    /// <summary>
    /// Верхняя граница TTL токена обмена, привязанного к практической сессии (<c>session_id</c>).
    /// Такой токен живёт до конца сессии (<c>SessionExpiresAt</c>), но не дольше этого значения;
    /// для сессии без лимита времени TTL равен ему. Токены без <c>session_id</c> по-прежнему
    /// ограничены <see cref="ExchangeAccessTokenMinutes"/>.
    /// </summary>
    public int SessionTokenMaxHours { get; init; } = 8;
}
