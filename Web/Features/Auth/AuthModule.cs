using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Features.Auth.Login;
using IdentityService.Web.Features.Auth.Logout;
using IdentityService.Web.Features.Auth.Refresh;
using IdentityService.Web.Features.Auth.Register;
using IdentityService.Web.Features.Auth.TokenExchange;

namespace IdentityService.Web.Features.Auth;

internal static class AuthModule
{
    internal static IServiceCollection AddAuth(this IServiceCollection services) =>
        services
            .AddScoped<IRequestHandler<RegisterCommand, Result<TokenResponse>>, RegisterHandler>()
            .AddScoped<IRequestHandler<LoginCommand, Result<TokenResponse>>, LoginHandler>()
            .AddScoped<IRequestHandler<RefreshCommand, Result<TokenResponse>>, RefreshHandler>()
            .AddScoped<IRequestHandler<LogoutCommand, Result>, LogoutHandler>()
            .AddScoped<IRequestHandler<TokenExchangeCommand, Result<TokenExchangeResponse>>, TokenExchangeHandler>();
}
