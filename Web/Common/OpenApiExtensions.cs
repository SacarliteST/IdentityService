using IdentityService.Contracts;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IdentityService.Web.Common;

internal static class OpenApiExtensions
{
    private const string BearerScheme = "Bearer";

    internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "IdentityService API",
                Version = "v1",
                Description = "Сервис аутентификации и авторизации платформы Scoodle."
            });

            c.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Введите JWT-токен доступа без префикса Bearer."
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
            });

            c.SupportNonNullableReferenceTypes();
            c.NonNullableReferenceTypesAsRequired();
            c.UseAllOfToExtendReferenceSchemas();

            foreach (var assembly in new[] { typeof(IWebMarker).Assembly, typeof(ApiRoutes).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                {
                    c.IncludeXmlComments(xml);
                }
            }
        });
}
