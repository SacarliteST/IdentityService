namespace IdentityService.Contracts;

/// <summary>
/// Запрос на обмен токена пользователя на токен, ограниченный аудиторией другого сервиса
/// (RFC 8693-style). Аутентификация вызывающего клиента — заголовком
/// <c>Authorization: Basic base64(client_id:client_secret)</c>, не телом запроса.
/// </summary>
/// <param name="GrantType">Всегда <c>urn:ietf:params:oauth:grant-type:token-exchange</c>.</param>
/// <param name="SubjectToken">Валидный токен пользователя, выданный этим инстансом.</param>
/// <param name="Audience">Целевая аудитория (должна быть в allow-list клиента).</param>
/// <param name="SessionId">
/// Необязательный идентификатор практической сессии (передаёт Education). При наличии
/// попадает в выпускаемый токен как claim <c>session_id</c> — бэкенд внешнего модуля
/// по нему привязывает вызовы к платформенной сессии.
/// </param>
/// <param name="SessionExpiresAt">
/// Необязательное время истечения практической сессии. С <paramref name="SessionId"/> токен живёт
/// до этого момента (но не дольше <c>Jwt:SessionTokenMaxHours</c>); без <paramref name="SessionId"/> —
/// <c>exp</c> обрезается по нему, если он раньше стандартного TTL обмена.
/// </param>
public sealed record TokenExchangeRequest(
    string GrantType,
    string SubjectToken,
    string Audience,
    string? SessionId = null,
    DateTimeOffset? SessionExpiresAt = null);
