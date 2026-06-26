using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityClientOptions>()
            .BindConfiguration(IdentityClientOptions.SectionKey);

        services.AddTransient<ErrorDelegatingHandler>();

        // Типизированные клиенты — в промте D

        return services;
    }
}
