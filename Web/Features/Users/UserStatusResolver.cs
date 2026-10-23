using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users;

internal static class UserStatusResolver
{
    internal static string Resolve(
        bool lockoutEnabled,
        DateTimeOffset? lockoutEnd,
        DateTimeOffset now,
        Domain.ApprovalState approval = Domain.ApprovalState.Approved)
    {
        if (lockoutEnabled && lockoutEnd > now)
        {
            return UserStatuses.Blocked;
        }

        return approval switch
        {
            Domain.ApprovalState.Pending => UserStatuses.Pending,
            Domain.ApprovalState.Rejected => UserStatuses.Rejected,
            _ => UserStatuses.Active,
        };
    }
}
