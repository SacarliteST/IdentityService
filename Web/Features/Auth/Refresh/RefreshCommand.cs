using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Refresh;

internal sealed record RefreshCommand(string RefreshToken)
    : IRequest<Result<TokenResponse>>;
