using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Web.Common.Tokens;

namespace IdentityService.Web.Features.Auth.Logout;

internal sealed class LogoutHandler(ITokenService tokenService)
    : IRequestHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand cmd, CancellationToken ct) =>
        tokenService.RevokeAsync(cmd.RefreshToken, ct);
}
