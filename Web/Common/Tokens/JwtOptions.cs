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
}
