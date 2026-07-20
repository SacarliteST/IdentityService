using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.CreateUser;

internal sealed class CreateUserHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<CreateUserCommand, Result<UserDetailsDto>>
{
    public async Task<Result<UserDetailsDto>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var user = new ApplicationUser
        {
            Email = command.Email,
            UserName = command.Email,
            DisplayName = command.DisplayName,
            CreatedAt = now
        };
        var roles = command.Roles
            .Select(role => role.ToString())
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            return Result<UserDetailsDto>.Fail(AuthErrors.FromIdentityErrors(createResult.Errors));
        }

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                return Result<UserDetailsDto>.Fail(AuthErrors.RoleNotFound(role));
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                return Result<UserDetailsDto>.Fail(AuthErrors.FromIdentityErrors(roleResult.Errors));
            }
        }

        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            user.Id,
            AuditEventTypes.UserCreated,
            $"Создан пользователь с ролями: {String.Join(", ", roles)}.",
            now));
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new UserDetailsDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            roles,
            UserStatuses.Active,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt,
            user.BlockedAt,
            user.BlockReason);
    }
}
