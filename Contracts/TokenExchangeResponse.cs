namespace IdentityService.Contracts;

/// <summary>
/// Ответ на обмен токена. Только access-токен — Token Exchange не выпускает refresh-токен,
/// обменянный токен одноразовый по смыслу и живёт ровно на длительность запуска.
/// </summary>
public sealed record TokenExchangeResponse(
    string AccessToken,
    int ExpiresIn);
