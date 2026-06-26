using IdentityService.Web;

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

            await app.InitializeWebAsync();
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
