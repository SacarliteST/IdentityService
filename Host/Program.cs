using IdentityService.Data;
using IdentityService.Data.Migrations;
using IdentityService.Data.Seeding;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Host;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var logger = Startup.CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        try
        {
            logger.LogInformation("The application has been started");
            Startup.ConfigureServices(builder);

            var app = builder.Build();
            Startup.ConfigureApp(app);

            using (var scope = app.Services.CreateScope())
            {
                var sp = scope.ServiceProvider;
                await sp.GetRequiredService<IMigrationManager>().MigrateAsync();
                await RoleSeeder.SeedRolesAsync(sp.GetRequiredService<RoleManager<ApplicationRole>>());
                await AdminSeeder.SeedAdminAsync(
                    sp.GetRequiredService<UserManager<ApplicationUser>>(),
                    builder.Configuration);
            }

            await app.RunAsync();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{exception} Host terminated unexpectedly");
            logger.LogError("{Exception} Host terminated unexpectedly", exception);
            throw;
        }
        finally
        {
            logger.LogInformation("Stopping application...");
        }
    }
}
