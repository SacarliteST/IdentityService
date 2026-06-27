using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.AssignRole;

internal sealed record AssignRoleCommand(Guid UserId, string Role) : IRequest<Result>;
