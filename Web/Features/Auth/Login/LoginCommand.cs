using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Login;

internal sealed record LoginCommand(string Email, string Password)
    : IRequest<Result<TokenResponse>>;
