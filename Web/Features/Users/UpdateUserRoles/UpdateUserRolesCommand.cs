using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.UpdateUserRoles;

internal sealed record UpdateUserRolesCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    IReadOnlyList<UserRole> Roles) : IRequest<Result>;
