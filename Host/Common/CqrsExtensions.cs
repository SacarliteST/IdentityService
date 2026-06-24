using IdentityService.Common.Cqrs;
using IdentityService.Host.Common.Behaviors;

namespace IdentityService.Host.Common;

internal static class CqrsExtensions
{
    internal static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
