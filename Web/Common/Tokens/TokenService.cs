using System.Security.Claims;
using System.Security.Cryptography;
using IdentityService.Common.Crypto;
using IdentityService.Common.Results;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common.Keys;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Web.Common.Tokens;

internal sealed class TokenService(
    ISigningKeyProvider signingKeyProvider,
    IOptions<JwtOptions> options,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public async Task<TokenPair> IssueAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var opts = options.Value;

        var accessExpiresAt = now.AddMinutes(opts.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, roles, opts.Audience, now, accessExpiresAt);

        var rawRefresh = GenerateRawRefreshToken();
        var refreshExpiresAt = now.AddDays(opts.RefreshTokenDays);

        var refreshEntity = RefreshToken.Create(
            user.Id,
            TokenHasher.Hash(rawRefresh),
            refreshExpiresAt,
            now);

        db.RefreshTokens.Add(refreshEntity);
        await db.SaveChangesAsync(ct);

        return new TokenPair(accessToken, accessExpiresAt, rawRefresh, refreshExpiresAt);
    }

    public Task<ExchangedAccessToken> IssueForAudienceAsync(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        string audience,
        string? sessionId = null,
        DateTimeOffset? sessionExpiresAt = null,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var opts = options.Value;

        // TTL токена практической сессии (есть session_id) — до конца сессии, но не дольше
        // SessionTokenMaxHours (для сессии без лимита времени это и есть TTL). Иначе студента
        // выбросило бы из модуля посреди попытки. Без session_id — стандартный TTL обмена,
        // обрезанный временем сессии, если оно передано и раньше.
        DateTimeOffset expiresAt;
        if (sessionId is not null)
        {
            expiresAt = now.AddHours(opts.SessionTokenMaxHours);
        }
        else
        {
            expiresAt = now.AddMinutes(opts.ExchangeAccessTokenMinutes);
        }

        if (sessionExpiresAt is { } sessionExpiry && sessionExpiry < expiresAt)
        {
            expiresAt = sessionExpiry;
        }

        // Token Exchange: короткоживущий токен под конкретный сервис.
        // Никакого refresh-токена не выпускается и в БД ничего не пишется — не пользовательская
        // сессия платформы, а разовый пропуск на длительность одного запуска модуля.
        var token = CreateAccessToken(user, roles, audience, now, expiresAt, sessionId);
        return Task.FromResult(new ExchangedAccessToken(token, expiresAt));
    }

    private string CreateAccessToken(
        ApplicationUser user,
        IReadOnlyList<string> roles,
        string audience,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        string? sessionId = null)
    {
        var opts = options.Value;

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("email", user.Email!),
            new("name", user.DisplayName ?? user.Email!),
            new("jti", Guid.NewGuid().ToString())
        };

        if (!String.IsNullOrEmpty(sessionId))
        {
            claims.Add(new Claim("session_id", sessionId));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = String.IsNullOrEmpty(opts.Issuer) ? null : opts.Issuer,
            Audience = String.IsNullOrEmpty(audience) ? null : audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingKeyProvider.GetSigningCredentials()
        };

        return TokenHandler.CreateToken(descriptor);
    }

    public async Task<Result<TokenPair>> RotateAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var hash = TokenHasher.Hash(rawRefreshToken);

        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
        {
            return Result<TokenPair>.Fail(AuthErrors.InvalidRefreshToken());
        }

        if (!token.IsActive(now))
        {
            if (token.RevokedAt is not null)
            {
                // Reuse-detection: revoke all active tokens to protect the account
                var active = await db.RefreshTokens
                    .Where(t => t.UserId == token.UserId && t.RevokedAt == null && t.ExpiresAt > now)
                    .ToListAsync(ct);

                foreach (var t in active)
                {
                    t.Revoke(now);
                }

                await db.SaveChangesAsync(ct);
            }

            return Result<TokenPair>.Fail(AuthErrors.InvalidRefreshToken());
        }

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
        {
            return Result<TokenPair>.Fail(AuthErrors.UserNotFound(token.UserId));
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            token.Revoke(now);
            await db.SaveChangesAsync(ct);
            return Result<TokenPair>.Fail(AuthErrors.UserBlocked());
        }

        var roles = await userManager.GetRolesAsync(user);
        var newPair = await IssueAsync(user, roles.ToList(), ct);

        token.Revoke(now, TokenHasher.Hash(newPair.RefreshToken));
        await db.SaveChangesAsync(ct);

        return newPair;
    }

    public async Task<Result> RevokeAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var hash = TokenHasher.Hash(rawRefreshToken);

        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.IsActive(now))
        {
            return Result.Success();
        }

        token.Revoke(now);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string GenerateRawRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes);
    }
}
