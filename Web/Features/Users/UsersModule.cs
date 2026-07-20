using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Features.Users.AssignRole;
using IdentityService.Web.Features.Users.GetUserDetails;
using IdentityService.Web.Features.Users.ListUsers;

namespace IdentityService.Web.Features.Users;

internal static class UsersModule
{
    internal static IServiceCollection AddUsers(this IServiceCollection services) =>
        services
            .AddScoped<IRequestHandler<AssignRoleCommand, Result>, AssignRoleHandler>()
            .AddScoped<
                IRequestHandler<ListUsersQuery, Result<PagedResponse<UserListItemDto>>>,
                ListUsersHandler>()
            .AddScoped<
                IRequestHandler<GetUserDetailsQuery, Result<UserDetailsDto>>,
                GetUserDetailsHandler>();
}
