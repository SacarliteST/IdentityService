using IdentityService.Common.Cqrs;
using IdentityService.Web.Common.Behaviors;

namespace IdentityService.Web.Common;

internal static class CqrsExtensions
{
    internal static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
