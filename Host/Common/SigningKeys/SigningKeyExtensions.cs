using IdentityService.Host.Common.Options;

namespace IdentityService.Host.Common.SigningKeys;

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
