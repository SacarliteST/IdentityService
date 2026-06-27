using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Web.Features.Auth.Logout;

internal sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
