using FluentValidation;
using IdentityService.Host.Common;

namespace IdentityService.Host;

internal static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocumentation();

        services.AddCqrs();
        services.AddFeatures();
        services.AddValidatorsFromAssemblyContaining<IHostMarker>(includeInternalTypes: true);
        services.AddEndpoints();
    }

    public static ILogger CreateLogger()
    {
        using var factory = LoggerFactory.Create(options => options
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace));

        return factory.CreateLogger<Program>();
    }

    public static void ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseApiExceptionHandler();
        app.MapEndpoints();
    }
}
