using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.ResetUserPassword;

internal sealed class ResetUserPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetUserPasswordCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Токен сброса не нужен (провайдеры токенов в сервисе не настроены); сложность пароля проверяет AddPasswordAsync,
        // при отказе транзакция откатывается вместе с удалением старого пароля.
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(removeResult.Errors));
        }

        var addResult = await userManager.AddPasswordAsync(user, command.NewPassword);
        if (!addResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(addResult.Errors));
        }

        user.UpdatedAt = now;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        // Старые сессии не должны пережить смену пароля.
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

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            AuditEventTypes.UserPasswordReset,
            "Пароль сброшен администратором.",
            now));
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
