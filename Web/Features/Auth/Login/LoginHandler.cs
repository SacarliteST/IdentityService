using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Auth.Login;

internal sealed class LoginHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(cmd.Email);

        if (user is null || !await userManager.CheckPasswordAsync(user, cmd.Password))
        {
            return Result<TokenResponse>.Fail(AuthErrors.InvalidCredentials());
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<TokenResponse>.Fail(AuthErrors.UserBlocked());
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.LastLoginAt = now;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<TokenResponse>.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        var roles = await userManager.GetRolesAsync(user);
        var pair = await tokenService.IssueAsync(user, roles.ToList(), ct);

        db.AuditEvents.Add(AuditEvent.Create(
            user.Id,
            user.Id,
            AuditEventTypes.LoginSucceeded,
            "Успешный вход пользователя.",
            now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new TokenResponse(
            pair.AccessToken, pair.AccessExpiresAt,
            pair.RefreshToken, pair.RefreshExpiresAt,
            user.Id, user.Email!, roles.ToList());
    }
}
