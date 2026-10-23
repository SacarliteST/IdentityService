using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<UpdateUserProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateUserProfileCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        var previous = String.Join(' ', new[] { user.LastName, user.FirstName, user.MiddleName }.Where(part => part is not null));
        var firstName = PersonName.Normalize(command.FirstName);
        var lastName = PersonName.Normalize(command.LastName);
        var middleName = PersonName.Normalize(command.MiddleName);
        var displayName = PersonName.BuildDisplayName(firstName, lastName);

        if (user.FirstName == firstName &&
            user.LastName == lastName &&
            user.MiddleName == middleName &&
            user.DisplayName == displayName)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        user.FirstName = firstName;
        user.LastName = lastName;
        user.MiddleName = middleName;
        user.DisplayName = displayName;
        user.UpdatedAt = now;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            command.BySelf ? AuditEventTypes.UserProfileSelfUpdated : AuditEventTypes.UserProfileUpdated,
            (command.BySelf ? "Пользователь изменил своё ФИО" : "ФИО изменено администратором") +
            $": {(previous.Length == 0 ? "—" : previous)} → " +
            String.Join(' ', new[] { lastName, firstName, middleName }.Where(part => part is not null)) + ".",
            now));
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
