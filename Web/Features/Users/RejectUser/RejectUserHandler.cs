using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Users.RejectUser;

internal sealed class RejectUserHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<RejectUserCommand, Result>
{
    public async Task<Result> Handle(RejectUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        if (user.ApprovalState == ApprovalState.Approved)
        {
            return Result.Fail(AuthErrors.CannotRejectApprovedUser());
        }

        if (user.ApprovalState == ApprovalState.Rejected)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.ApprovalState = ApprovalState.Rejected;
        user.UpdatedAt = now;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(update.Errors));
        }

        var reason = PersonName.Normalize(command.Reason);
        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            AuditEventTypes.UserRejected,
            $"Заявка на регистрацию отклонена ({user.Email})" + (reason is null ? "." : $": {reason}."),
            now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
