using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConnectionOptions>()
            .BindConfiguration(ConnectionOptions.SectionKey);

        // AppDbContext + Identity stores + миграции — в промте A

        return services;
    }
}
