using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.RejectUser;

internal sealed record RejectUserCommand(Guid ActorUserId, Guid TargetUserId, string? Reason) : IRequest<Result>;
