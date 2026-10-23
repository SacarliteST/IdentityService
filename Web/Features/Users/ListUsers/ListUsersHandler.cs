using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users.ListUsers;

internal sealed class ListUsersHandler(
    AppDbContext db,
    TimeProvider timeProvider)
    : IRequestHandler<ListUsersQuery, Result<PagedResponse<UserListItemDto>>>
{
    public async Task<Result<PagedResponse<UserListItemDto>>> Handle(
        ListUsersQuery query,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var users = db.Users.AsNoTracking();

        if (!String.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            users = users.Where(user =>
                (user.Email != null && EF.Functions.ILike(user.Email, pattern)) ||
                (user.DisplayName != null && EF.Functions.ILike(user.DisplayName, pattern)) ||
                (user.LastName != null && EF.Functions.ILike(user.LastName, pattern)) ||
                (user.FirstName != null && EF.Functions.ILike(user.FirstName, pattern)));
        }

        if (!String.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = query.Role.Trim().ToUpperInvariant();
            var userIdsInRole =
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                where role.NormalizedName == normalizedRole
                select userRole.UserId;

            users = users.Where(user => userIdsInRole.Contains(user.Id));
        }

        if (String.Equals(query.Status, UserStatuses.Blocked, StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user =>
                user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > now);
        }
        else if (String.Equals(query.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user =>
                (!user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= now)
                && user.ApprovalState == ApprovalState.Approved);
        }
        else if (String.Equals(query.Status, UserStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => user.ApprovalState == ApprovalState.Pending);
        }
        else if (String.Equals(query.Status, UserStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(user => user.ApprovalState == ApprovalState.Rejected);
        }

        var totalCount = await users.CountAsync(ct);
        var pageUsers = await users
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(user => new
            {
                user.Id,
                Email = user.Email!,
                user.DisplayName,
                user.FirstName,
                user.LastName,
                user.MiddleName,
                user.CreatedAt,
                user.LastLoginAt,
                user.LockoutEnabled,
                user.LockoutEnd,
                user.ApprovalState
            })
            .ToListAsync(ct);

        var userIds = pageUsers.Select(user => user.Id).ToList();
        var roleRows = userIds.Count == 0
            ? []
            : await (
                from userRole in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                orderby role.Name
                select new { userRole.UserId, Role = role.Name! })
                .ToListAsync(ct);

        var rolesByUser = roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => row.Role).ToList());

        var items = pageUsers
            .Select(user => new UserListItemDto(
                user.Id,
                user.Email,
                user.DisplayName,
                rolesByUser.GetValueOrDefault(user.Id) ?? [],
                UserStatusResolver.Resolve(user.LockoutEnabled, user.LockoutEnd, now, user.ApprovalState),
                user.CreatedAt,
                user.LastLoginAt,
                user.FirstName,
                user.LastName,
                user.MiddleName))
            .ToList();

        return new PagedResponse<UserListItemDto>(items, query.Page, query.PageSize, totalCount);
    }
}
