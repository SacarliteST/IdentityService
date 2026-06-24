using IdentityService.Data.Migrations;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConnectionOptions>()
            .BindConfiguration(ConnectionOptions.SectionKey);

        var connectionString = configuration.GetConnectionString("ConnectionString")
            ?? throw new InvalidOperationException("ConnectionStrings:ConnectionString is not configured.");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(connectionString));

        services.AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireUppercase = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IMigrationManager, DatabaseMigrationManager>();

        return services;
    }
}
