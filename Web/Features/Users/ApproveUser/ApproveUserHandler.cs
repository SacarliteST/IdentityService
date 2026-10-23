using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Users.ApproveUser;

internal sealed class ApproveUserHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<ApproveUserCommand, Result>
{
    public async Task<Result> Handle(ApproveUserCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        if (user.ApprovalState == ApprovalState.Approved)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.ApprovalState = ApprovalState.Approved;
        user.ApprovedAt = now;
        user.ApprovedByUserId = command.ActorUserId;
        user.UpdatedAt = now;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(update.Errors));
        }

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            AuditEventTypes.UserApproved,
            $"Заявка на регистрацию одобрена ({user.Email}).",
            now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
