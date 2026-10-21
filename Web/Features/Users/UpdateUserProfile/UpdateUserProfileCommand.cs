using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Users.UpdateUserProfile;

internal sealed record UpdateUserProfileCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    string? FirstName,
    string? LastName,
    string? MiddleName) : IRequest<Result>;
