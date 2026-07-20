using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.BlockUser;

internal sealed class BlockUserHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<BlockUserCommand, Result>
{
    public async Task<Result> Handle(BlockUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        if (command.ActorUserId == command.TargetUserId)
        {
            return Result.Fail(AuthErrors.CannotBlockSelf());
        }

        var now = timeProvider.GetUtcNow();
        if (user.LockoutEnabled && user.LockoutEnd > now)
        {
            return Result.Success();
        }

        if (await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
            var activeAdminCount = admins.Count(admin =>
                !admin.LockoutEnabled || !admin.LockoutEnd.HasValue || admin.LockoutEnd <= now);

            if (activeAdminCount <= 1)
            {
                return Result.Fail(AuthErrors.CannotBlockLastActiveAdmin());
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.BlockedAt = now;
        user.BlockReason = command.Reason;
        user.UpdatedAt = now;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        var activeTokens = await db.RefreshTokens
            .Where(token =>
                token.UserId == user.Id &&
                token.RevokedAt == null &&
                token.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
