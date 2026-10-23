using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Features.Me;
using IdentityService.Web.Features.Users.ApproveUser;
using IdentityService.Web.Features.Users.BlockUser;
using IdentityService.Web.Features.Users.CreateUser;
using IdentityService.Web.Features.Users.GetUserDetails;
using IdentityService.Web.Features.Users.ListUsers;
using IdentityService.Web.Features.Users.RejectUser;
using IdentityService.Web.Features.Users.ResetUserPassword;
using IdentityService.Web.Features.Users.UnblockUser;
using IdentityService.Web.Features.Users.UpdateUserProfile;
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
            .AddScoped<IRequestHandler<UpdateUserProfileCommand, Result>, UpdateUserProfileHandler>()
            .AddScoped<IRequestHandler<ApproveUserCommand, Result>, ApproveUserHandler>()
            .AddScoped<IRequestHandler<RejectUserCommand, Result>, RejectUserHandler>()
            .AddScoped<IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>, GetMyProfileHandler>()
            .AddScoped<IRequestHandler<ChangeMyPasswordCommand, Result>, ChangeMyPasswordHandler>()
            .AddScoped<IRequestHandler<ListInviteCodesQuery, Result<IReadOnlyList<InviteCodeDto>>>, ListInviteCodesHandler>()
            .AddScoped<IRequestHandler<CreateInviteCodeCommand, Result<InviteCodeDto>>, CreateInviteCodeHandler>()
            .AddScoped<IRequestHandler<DeactivateInviteCodeCommand, Result>, DeactivateInviteCodeHandler>()
            .AddScoped<IRequestHandler<ResetUserPasswordCommand, Result>, ResetUserPasswordHandler>()
            .AddScoped<
                IRequestHandler<ListUsersQuery, Result<PagedResponse<UserListItemDto>>>,
                ListUsersHandler>()
            .AddScoped<
                IRequestHandler<GetUserDetailsQuery, Result<UserDetailsDto>>,
                GetUserDetailsHandler>();
}
