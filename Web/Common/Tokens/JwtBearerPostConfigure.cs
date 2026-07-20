using System.Security.Claims;
using IdentityService.Web.Common.Keys;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Web.Common.Tokens;

/// <summary>
/// Настраивает <see cref="JwtBearerOptions"/> после сборки DI-контейнера,
/// используя тот же ключ <see cref="ISigningKeyProvider"/>, которым сервис подписывает токены.
/// Публичная часть ключа по-прежнему публикуется через JWKS для внешних сервисов.
/// </summary>
internal sealed class JwtBearerPostConfigure(
    ISigningKeyProvider keyProvider,
    IOptions<JwtOptions> jwtOptions) : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var opts = jwtOptions.Value;
        var signingKey = keyProvider.GetSigningCredentials().Key;

        options.RequireHttpsMetadata = false; // dev — enable in prod
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = signingKey,
            ValidIssuer = String.IsNullOrEmpty(opts.Issuer) ? null : opts.Issuer,
            ValidAudience = String.IsNullOrEmpty(opts.Audience) ? null : opts.Audience,
            ValidateIssuer = !String.IsNullOrEmpty(opts.Issuer),
            ValidateAudience = !String.IsNullOrEmpty(opts.Audience),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    }
}
