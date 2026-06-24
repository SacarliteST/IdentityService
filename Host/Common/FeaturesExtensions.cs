namespace IdentityService.Host.Common;

internal static class FeaturesExtensions
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        // Каждый XxxModule.AddXxx() добавляется сюда одной строкой
        return services;
    }
}
