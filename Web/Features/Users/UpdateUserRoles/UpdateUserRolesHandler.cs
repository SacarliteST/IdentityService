using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.UpdateUserRoles;

internal sealed class UpdateUserRolesHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<UpdateUserRolesCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRolesCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.TargetUserId));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var now = timeProvider.GetUtcNow();
        var requestedRoles = command.Roles
            .Select(role => role.ToString())
            .ToHashSet(StringComparer.Ordinal);
        var removesAdmin = currentRoles.Contains(RoleNames.Admin) &&
            !requestedRoles.Contains(RoleNames.Admin);

        if (removesAdmin && command.ActorUserId == command.TargetUserId)
        {
            return Result.Fail(AuthErrors.CannotRemoveOwnAdminRole());
        }

        if (removesAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
            var activeAdminCount = admins.Count(admin =>
                !admin.LockoutEnabled || !admin.LockoutEnd.HasValue || admin.LockoutEnd <= now);

            if (activeAdminCount <= 1)
            {
                return Result.Fail(AuthErrors.CannotRemoveLastAdminRole());
            }
        }

        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.Ordinal).ToList();
        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.Ordinal).ToList();
        if (rolesToRemove.Count == 0 && rolesToAdd.Count == 0)
        {
            return Result.Success();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return Result.Fail(AuthErrors.FromIdentityErrors(removeResult.Errors));
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return Result.Fail(AuthErrors.FromIdentityErrors(addResult.Errors));
            }
        }

        user.UpdatedAt = now;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(updateResult.Errors));
        }

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            command.TargetUserId,
            AuditEventTypes.UserRolesUpdated,
            $"Роли изменены: {String.Join(", ", currentRoles.Order())} -> " +
            $"{String.Join(", ", requestedRoles.Order())}.",
            now));
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
