using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.BlockUser;

internal sealed record BlockUserCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    string? Reason) : IRequest<Result>;
