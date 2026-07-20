using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.UnblockUser;

internal sealed class UnblockUserHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<UnblockUserCommand, Result>
{
    public async Task<Result> Handle(UnblockUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        var now = timeProvider.GetUtcNow();
        var isBlocked = user.LockoutEnabled && user.LockoutEnd > now;
        if (!isBlocked && user.BlockedAt is null && user.BlockReason is null)
        {
            return Result.Success();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.LockoutEnd = null;
        user.BlockedAt = null;
        user.BlockReason = null;
        user.UpdatedAt = now;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            AuditEventTypes.UserUnblocked,
            "Пользователь разблокирован.",
            now));
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
