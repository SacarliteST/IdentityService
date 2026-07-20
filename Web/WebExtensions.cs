using FluentValidation;
using IdentityService.Data;
using IdentityService.Data.Migrations;
using IdentityService.Data.Seeding;
using IdentityService.Domain;
using IdentityService.Web.Common;
using IdentityService.Web.Common.Keys;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityService.Web;

/// <summary>
/// Единая точка подключения Web-слоя. Host вызывает только эти методы.
/// </summary>
public static class WebExtensions
{
    private const string FrontendCorsPolicy = "Frontend";

    /// <summary>Регистрирует все сервисы Web-слоя (данные, CQRS, эндпоинты, ключи, токены, OpenAPI).</summary>
    public static IServiceCollection AddWeb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocumentation();
        services.AddHttpContextAccessor();
        services.AddCors(options =>
        {
            var origins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        services.AddData(configuration);
        services.AddSigningKeys(configuration);
        services.AddTokens();
        services.AddCqrs();
        services.AddFeatures();
        services.AddValidatorsFromAssemblyContaining<IWebMarker>(includeInternalTypes: true);
        services.AddEndpoints();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigure>();
        services.AddAuthorization();

        return services;
    }

    /// <summary>Подключает middleware Web-слоя (исключения, Swagger, auth, маршруты).</summary>
    public static WebApplication UseWeb(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseApiExceptionHandler();
        app.UseCors(FrontendCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapEndpoints();

        return app;
    }

    /// <summary>Применяет миграции и засевает начальные роли и пользователей.</summary>
    public static async Task InitializeWebAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<IMigrationManager>().MigrateAsync();
        await RoleSeeder.SeedRolesAsync(sp.GetRequiredService<RoleManager<ApplicationRole>>());
        await UserSeeder.SeedUsersAsync(
            sp.GetRequiredService<UserManager<ApplicationUser>>(),
            app.Configuration,
            sp.GetRequiredService<TimeProvider>());
    }
}
