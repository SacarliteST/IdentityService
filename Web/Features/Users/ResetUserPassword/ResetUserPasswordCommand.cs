using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.ResetUserPassword;

internal sealed record ResetUserPasswordCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    string NewPassword) : IRequest<Result>;
