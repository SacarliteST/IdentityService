using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService) : IRequestHandler<RegisterCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            Email = cmd.Email,
            UserName = cmd.Email,
            DisplayName = cmd.DisplayName
        };

        var result = await userManager.CreateAsync(user, cmd.Password);
        if (!result.Succeeded)
        {
            return Result<TokenResponse>.Fail(AuthErrors.FromIdentityErrors(result.Errors));
        }

        await userManager.AddToRoleAsync(user, RoleNames.Student);

        var pair = await tokenService.IssueAsync(user, [RoleNames.Student], ct);

        return new TokenResponse(
            pair.AccessToken, pair.AccessExpiresAt,
            pair.RefreshToken, pair.RefreshExpiresAt,
            user.Id, user.Email!, [RoleNames.Student]);
    }
}
