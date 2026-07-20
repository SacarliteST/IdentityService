using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Auth.Login;

internal sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(cmd.Email);

        // Не раскрываем, что именно неверно: email или пароль
        if (user is null || !await userManager.CheckPasswordAsync(user, cmd.Password))
        {
            return Result<TokenResponse>.Fail(AuthErrors.InvalidCredentials());
        }

        user.LastLoginAt = timeProvider.GetUtcNow();
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<TokenResponse>.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        var roles = await userManager.GetRolesAsync(user);
        var pair = await tokenService.IssueAsync(user, roles.ToList(), ct);

        return new TokenResponse(
            pair.AccessToken, pair.AccessExpiresAt,
            pair.RefreshToken, pair.RefreshExpiresAt,
            user.Id, user.Email!, roles.ToList());
    }
}
