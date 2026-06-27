using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IdentityService.Web.Features.Auth.Refresh;

internal sealed class RefreshHandler(
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager) : IRequestHandler<RefreshCommand, Result<TokenResponse>>
{
    private static readonly JsonWebTokenHandler JwtReader = new();

    public async Task<Result<TokenResponse>> Handle(RefreshCommand cmd, CancellationToken ct)
    {
        var rotateResult = await tokenService.RotateAsync(cmd.RefreshToken, ct);
        if (!rotateResult.IsSuccess)
        {
            return Result<TokenResponse>.Fail(rotateResult.Error!);
        }

        var pair = rotateResult.Value!;

        // Extract identity from freshly-issued access token (no signature check needed here)
        var jwt = JwtReader.ReadJsonWebToken(pair.AccessToken);
        var userId = jwt.Subject;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<TokenResponse>.Fail(AuthErrors.UserNotFound(userId));
        }

        var roles = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return new TokenResponse(
            pair.AccessToken, pair.AccessExpiresAt,
            pair.RefreshToken, pair.RefreshExpiresAt,
            user.Id, user.Email!, roles);
    }
}
