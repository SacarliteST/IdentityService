using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.UnblockUser;

internal sealed record UnblockUserCommand(Guid TargetUserId) : IRequest<Result>;
