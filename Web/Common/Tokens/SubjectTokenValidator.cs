using System.Security.Claims;
using IdentityService.Web.Common.Keys;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Web.Common.Tokens;

internal sealed class SubjectTokenValidator(
    ISigningKeyProvider signingKeyProvider,
    IOptions<JwtOptions> options) : ISubjectTokenValidator
{
    private static readonly JsonWebTokenHandler Handler = new();

    public async Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken ct = default)
    {
        var opts = options.Value;

        // Те же правила, что JwtBearerPostConfigure применяет к входящим запросам:
        // subjectToken должен быть валидным токеном ЭТОГО инстанса (обычная Audience,
        // не уже обменянная под какой-то другой сервис) — иначе цепочка обменов
        // могла бы расширяться бесконтрольно.
        var parameters = new TokenValidationParameters
        {
            IssuerSigningKey = signingKeyProvider.GetSigningCredentials().Key,
            ValidIssuer = String.IsNullOrEmpty(opts.Issuer) ? null : opts.Issuer,
            ValidAudience = String.IsNullOrEmpty(opts.Audience) ? null : opts.Audience,
            ValidateIssuer = !String.IsNullOrEmpty(opts.Issuer),
            ValidateAudience = !String.IsNullOrEmpty(opts.Audience),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        var result = await Handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid || result.ClaimsIdentity is null)
        {
            return null;
        }

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }
}
