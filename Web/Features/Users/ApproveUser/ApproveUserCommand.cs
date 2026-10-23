using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.ApproveUser;

internal sealed record ApproveUserCommand(Guid ActorUserId, Guid TargetUserId) : IRequest<Result>;
