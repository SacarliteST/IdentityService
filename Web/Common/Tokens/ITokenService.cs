using IdentityService.Common.Results;
using IdentityService.Domain;

namespace IdentityService.Web.Common.Tokens;

/// <summary>
/// Сервис выпуска и управления JWT access / refresh токенами.
/// <br/>
/// Refresh-токены хранятся в БД в виде хэша (SHA-256 base64url).
/// Поддерживает ротацию с reuse-detection: повторное использование отозванного токена
/// инициирует отзыв всех активных токенов пользователя.
/// </summary>
internal interface ITokenService
{
    /// <summary>
    /// Выпускает новую пару access/refresh токенов для указанного пользователя.
    /// Access-токен подписывается RS256 через <see cref="Keys.ISigningKeyProvider"/>.
    /// Refresh-токен сохраняется в БД (только хэш).
    /// </summary>
    Task<TokenPair> IssueAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        CancellationToken ct = default);

    /// <summary>
    /// Выпускает короткоживущий access-токен, ограниченный указанной <paramref name="audience"/>,
    /// вместо стандартной для этого инстанса. Refresh-токен не выпускается, в БД ничего не
    /// сохраняется — используется Token Exchange (<c>POST /auth/token/exchange</c>) для выдачи
    /// доверенным клиентом токена под целевой сервис (например, внешний практический модуль).
    /// <paramref name="sessionId"/> при наличии попадает в claim <c>session_id</c>;
    /// TTL: с <paramref name="sessionId"/> — до <paramref name="sessionExpiresAt"/>, но не дольше
    /// <c>Jwt:SessionTokenMaxHours</c> (без <paramref name="sessionExpiresAt"/> — ровно столько);
    /// без <paramref name="sessionId"/> — <c>Jwt:ExchangeAccessTokenMinutes</c>, обрезанный
    /// <paramref name="sessionExpiresAt"/>, если тот раньше.
    /// </summary>
    Task<ExchangedAccessToken> IssueForAudienceAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        string audience,
        string? sessionId = null,
        DateTimeOffset? sessionExpiresAt = null,
        CancellationToken ct = default);

    /// <summary>
    /// Ротирует refresh-токен: отзывает старый, выпускает новую пару.
    /// Возвращает <c>Fail(InvalidRefreshToken 401)</c> если токен не найден, истёк или уже отозван.
    /// При reuse-detection (повтор отозванного токена) отзывает ВСЕ активные сессии пользователя.
    /// </summary>
    Task<Result<TokenPair>> RotateAsync(string rawRefreshToken, CancellationToken ct = default);

    /// <summary>
    /// Отзывает refresh-токен. Идемпотентно: токен не найден или уже отозван → Success.
    /// </summary>
    Task<Result> RevokeAsync(string rawRefreshToken, CancellationToken ct = default);
}
