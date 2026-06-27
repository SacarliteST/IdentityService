using System.Security.Claims;
using System.Security.Cryptography;
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

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("email", user.Email!),
            new("name", user.DisplayName ?? user.Email!),
            new("jti", Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = String.IsNullOrEmpty(opts.Issuer) ? null : opts.Issuer,
            Audience = String.IsNullOrEmpty(opts.Audience) ? null : opts.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(opts.AccessTokenMinutes).UtcDateTime,
            SigningCredentials = signingKeyProvider.GetSigningCredentials()
        };

        var accessToken = TokenHandler.CreateToken(descriptor);
        var accessExpiresAt = now.AddMinutes(opts.AccessTokenMinutes);

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
