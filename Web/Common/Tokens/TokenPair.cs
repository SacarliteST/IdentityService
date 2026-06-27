namespace IdentityService.Web.Common.Tokens;

/// <summary>Пара токенов, возвращаемая при выпуске или ротации. Внутренний тип — не является публичным контрактом.</summary>
internal sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);
