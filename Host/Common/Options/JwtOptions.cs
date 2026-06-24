namespace IdentityService.Host.Common.Options;

public sealed class JwtOptions
{
    public const string SectionKey = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
