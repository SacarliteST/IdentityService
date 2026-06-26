using IdentityService.Web.Common.Tokens;

namespace IdentityService.Web.Common.Keys;

internal static class SigningKeyExtensions
{
    internal static IServiceCollection AddSigningKeys(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionKey);
        services.AddOptions<SigningKeyOptions>().BindConfiguration(SigningKeyOptions.SectionKey);
        services.AddSingleton<ISigningKeyProvider, RsaSigningKeyProvider>();
        return services;
    }
}
