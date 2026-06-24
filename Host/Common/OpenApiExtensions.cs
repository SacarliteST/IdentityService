using IdentityService.Contracts;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IdentityService.Host.Common;

internal static class OpenApiExtensions
{
    internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services) =>
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "IdentityService API",
                Version = "v1",
                Description = "Сервис аутентификации и авторизации платформы Scoodle."
            });

            c.SupportNonNullableReferenceTypes();
            c.UseAllOfToExtendReferenceSchemas();

            foreach (var assembly in new[] { typeof(IHostMarker).Assembly, typeof(ApiRoutes).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                {
                    c.IncludeXmlComments(xml);
                }
            }
        });
}
