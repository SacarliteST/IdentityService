namespace IdentityService.Contracts;

/// <summary>Ответ с парой JWT/refresh токенов и данными аутентифицированного пользователя.</summary>
public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles);
