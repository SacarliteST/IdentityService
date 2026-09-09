using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.TokenExchange;

internal sealed record TokenExchangeCommand(
    string ClientId,
    string ClientSecret,
    string SubjectToken,
    string Audience,
    string? SessionId,
    DateTimeOffset? SessionExpiresAt)
    : IRequest<Result<TokenExchangeResponse>>;
