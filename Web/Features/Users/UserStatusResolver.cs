using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users;

internal static class UserStatusResolver
{
    internal static string Resolve(
        bool lockoutEnabled,
        DateTimeOffset? lockoutEnd,
        DateTimeOffset now) =>
        lockoutEnabled && lockoutEnd > now
            ? UserStatuses.Blocked
            : UserStatuses.Active;
}
