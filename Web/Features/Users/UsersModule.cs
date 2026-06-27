using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Web.Features.Users.AssignRole;

namespace IdentityService.Web.Features.Users;

internal static class UsersModule
{
    internal static IServiceCollection AddUsers(this IServiceCollection services) =>
        services.AddScoped<IRequestHandler<AssignRoleCommand, Result>, AssignRoleHandler>();
}
