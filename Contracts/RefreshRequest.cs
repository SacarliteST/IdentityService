namespace IdentityService.Contracts;

/// <summary>Запрос на обновление токенов по refresh-токену.</summary>
public sealed record RefreshRequest(string RefreshToken);
