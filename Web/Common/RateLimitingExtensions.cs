using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace IdentityService.Web.Common;

/// <summary>Ограничение частоты запросов к входу, регистрации и смене пароля (по IP клиента).</summary>
internal static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // За шлюзом настоящий адрес клиента приходит в X-Forwarded-For (его дописывает nginx — берётся последний).
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Настройки читаются в момент первого запроса клиента, а не при старте: так их можно переопределить в тестах.
            options.AddPolicy(AuthPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IConfiguration>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.GetValue("RateLimiting:AuthPermitLimit", 20),
                        Window = TimeSpan.FromSeconds(settings.GetValue("RateLimiting:AuthWindowSeconds", 60)),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }
}
