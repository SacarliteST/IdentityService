using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed record RegisterCommand(string Email, string Password, string? DisplayName)
    : IRequest<Result<TokenResponse>>;
