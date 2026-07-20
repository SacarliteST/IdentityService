using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Features.Users.BlockUser;
using IdentityService.Web.Features.Users.CreateUser;
using IdentityService.Web.Features.Users.GetUserDetails;
using IdentityService.Web.Features.Users.ListUsers;
using IdentityService.Web.Features.Users.UnblockUser;
using IdentityService.Web.Features.Users.UpdateUserRoles;

namespace IdentityService.Web.Features.Users;

internal static class UsersModule
{
    internal static IServiceCollection AddUsers(this IServiceCollection services) =>
        services
            .AddScoped<IRequestHandler<BlockUserCommand, Result>, BlockUserHandler>()
            .AddScoped<IRequestHandler<UnblockUserCommand, Result>, UnblockUserHandler>()
            .AddScoped<
                IRequestHandler<CreateUserCommand, Result<UserDetailsDto>>,
                CreateUserHandler>()
            .AddScoped<IRequestHandler<UpdateUserRolesCommand, Result>, UpdateUserRolesHandler>()
            .AddScoped<
                IRequestHandler<ListUsersQuery, Result<PagedResponse<UserListItemDto>>>,
                ListUsersHandler>()
            .AddScoped<
                IRequestHandler<GetUserDetailsQuery, Result<UserDetailsDto>>,
                GetUserDetailsHandler>();
}
