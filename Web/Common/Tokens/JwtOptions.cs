namespace IdentityService.Web.Common.Tokens;

public sealed class JwtOptions
{
    public const string SectionKey = "Jwt";

    public string Issuer { get; init; } = String.Empty;
    public string Audience { get; init; } = String.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
