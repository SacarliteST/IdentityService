using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace IdentityService.IntegrationTests.Infrastructure;

/// <summary>Пропускает тест если Docker недоступен или не отвечает.</summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available or misconfigured in this environment.";
        }
    }
}

/// <summary>Пропускает тест если Docker недоступен или не отвечает.</summary>
public sealed class DockerTheoryAttribute : TheoryAttribute
{
    public DockerTheoryAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available or misconfigured in this environment.";
        }
    }
}

internal static class DockerAvailability
{
    // Вычисляется один раз до обнаружения тестов.
    // Build() внутри вызывает Validate() → реальный Docker API ping.
    public static readonly bool IsAvailable = CheckDocker();

    private static bool CheckDocker()
    {
        try
        {
            _ = new PostgreSqlBuilder().Build();
            return true;
        }
        catch (DockerUnavailableException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}
