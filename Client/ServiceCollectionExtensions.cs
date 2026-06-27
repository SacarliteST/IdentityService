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

        services.AddHttpClient<IIdentityClient, IdentityClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityClientOptions>>().Value;
                client.BaseAddress = opts.BaseAddress;
            })
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        return services;
    }
}
