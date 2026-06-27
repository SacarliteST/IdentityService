using IdentityService.Web.Features.Auth;
using IdentityService.Web.Features.Users;

namespace IdentityService.Web.Common;

internal static class FeaturesExtensions
{
    internal static IServiceCollection AddFeatures(this IServiceCollection services) =>
        services
            .AddAuth()
            .AddUsers();
}
