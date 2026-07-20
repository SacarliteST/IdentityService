using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.GetUserDetails;

internal sealed record GetUserDetailsQuery(Guid UserId) : IRequest<Result<UserDetailsDto>>;
