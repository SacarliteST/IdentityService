using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed class RegisterHandler(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    AppDbContext db,
    IOptions<RegistrationOptions> options,
    TimeProvider timeProvider) : IRequestHandler<RegisterCommand, Result<RegisterResult>>
{
    public async Task<Result<RegisterResult>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var mode = options.Value.Mode;
        if (mode == RegistrationMode.Closed)
        {
            return Result<RegisterResult>.Fail(AuthErrors.RegistrationClosed());
        }

        var now = timeProvider.GetUtcNow();
        var code = String.IsNullOrWhiteSpace(cmd.InviteCode) ? null : InviteCode.Normalize(cmd.InviteCode);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        InviteCode? invite = null;
        if (code is not null)
        {
            invite = await db.InviteCodes.AsNoTracking().FirstOrDefaultAsync(item => item.Code == code, ct);
            if (invite is null || !invite.IsUsable(now))
            {
                return Result<RegisterResult>.Fail(AuthErrors.InvalidInviteCode());
            }

            // Использование считается одним условным UPDATE: два параллельных запроса не потратят последний слот дважды.
            var used = await db.InviteCodes
                .Where(item => item.Id == invite.Id
                    && item.IsActive
                    && (item.ExpiresAt == null || item.ExpiresAt > now)
                    && (item.MaxUses == null || item.UsedCount < item.MaxUses))
                .ExecuteUpdateAsync(set => set.SetProperty(item => item.UsedCount, item => item.UsedCount + 1), ct);
            if (used == 0)
            {
                return Result<RegisterResult>.Fail(AuthErrors.InvalidInviteCode());
            }
        }

        var approved = mode == RegistrationMode.Open || invite is not null;
        var firstName = PersonName.Normalize(cmd.FirstName);
        var lastName = PersonName.Normalize(cmd.LastName);
        var user = new ApplicationUser
        {
            Email = cmd.Email,
            UserName = cmd.Email,
            FirstName = firstName,
            LastName = lastName,
            MiddleName = PersonName.Normalize(cmd.MiddleName),
            DisplayName = String.IsNullOrWhiteSpace(cmd.DisplayName)
                ? PersonName.BuildDisplayName(firstName, lastName)
                : cmd.DisplayName,
            CreatedAt = now,
            ApprovalState = approved ? ApprovalState.Approved : ApprovalState.Pending,
            ApprovedAt = approved ? now : null,
            InviteCodeId = invite?.Id,
        };

        var result = await userManager.CreateAsync(user, cmd.Password);
        if (!result.Succeeded)
        {
            return Result<RegisterResult>.Fail(AuthErrors.FromIdentityErrors(result.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Student);
        if (!roleResult.Succeeded)
        {
            return Result<RegisterResult>.Fail(AuthErrors.FromIdentityErrors(roleResult.Errors));
        }

        db.AuditEvents.Add(AuditEvent.Create(
            user.Id,
            user.Id,
            AuditEventTypes.UserRegistered,
            "Самостоятельная регистрация; " +
            (approved ? "доступ открыт сразу" : "ожидает одобрения администратора") +
            (invite is not null ? $"; код приглашения {invite.Code}" : String.Empty) + ".",
            now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (!approved)
        {
            return new RegisterResult(null, UserStatuses.Pending, user.Email!);
        }

        var pair = await tokenService.IssueAsync(user, [RoleNames.Student], ct);
        var tokens = new TokenResponse(
            pair.AccessToken, pair.AccessExpiresAt,
            pair.RefreshToken, pair.RefreshExpiresAt,
            user.Id, user.Email!, [RoleNames.Student]);
        return new RegisterResult(tokens, UserStatuses.Active, user.Email!);
    }
}
