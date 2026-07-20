using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.CreateUser;

internal sealed record CreateUserCommand(
    Guid ActorUserId,
    string Email,
    string? DisplayName,
    string Password,
    IReadOnlyList<UserRole> Roles) : IRequest<Result<UserDetailsDto>>;
