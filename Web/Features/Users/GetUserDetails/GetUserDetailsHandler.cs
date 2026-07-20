using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Web.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.GetUserDetails;

internal sealed class GetUserDetailsHandler(
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<GetUserDetailsQuery, Result<UserDetailsDto>>
{
    public async Task<Result<UserDetailsDto>> Handle(GetUserDetailsQuery query, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == query.UserId)
            .Select(candidate => new
            {
                candidate.Id,
                Email = candidate.Email!,
                candidate.DisplayName,
                candidate.CreatedAt,
                candidate.UpdatedAt,
                candidate.LastLoginAt,
                candidate.BlockedAt,
                candidate.BlockReason,
                candidate.LockoutEnabled,
                candidate.LockoutEnd
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return Result<UserDetailsDto>.Fail(AuthErrors.UserNotFound(query.UserId));
        }

        var roles = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == query.UserId
            orderby role.Name
            select role.Name!)
            .ToListAsync(ct);

        return new UserDetailsDto(
            user.Id,
            user.Email,
            user.DisplayName,
            roles,
            UserStatusResolver.Resolve(
                user.LockoutEnabled,
                user.LockoutEnd,
                timeProvider.GetUtcNow()),
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt,
            user.BlockedAt,
            user.BlockReason);
    }
}
