namespace IdentityService.Web.Common.Tokens;

internal static class TokenExtensions
{
    internal static IServiceCollection AddTokens(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }
}
