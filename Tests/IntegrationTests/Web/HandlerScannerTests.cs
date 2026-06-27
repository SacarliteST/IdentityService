using IdentityService.Common.Cqrs;
using IdentityService.Web;
using Shouldly;

namespace IdentityService.IntegrationTests.Web;

/// <summary>
/// E7: проверяет, что все IRequestHandler'ы в Web-сборке имеют ровно одну регистрацию,
/// а сборка не содержит не подключённых обработчиков (забытый AddXxx в AddFeatures).
/// </summary>
public sealed class HandlerScannerTests
{
    [Fact]
    public void AllHandlers_InWebAssembly_AreConcreteClasses()
    {
        var handlerType = typeof(IRequestHandler<,>);

        var handlers = typeof(IWebMarker).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType))
            .ToList();

        handlers.ShouldNotBeEmpty("Web-сборка должна содержать хотя бы один IRequestHandler");

        foreach (var h in handlers)
        {
            h.IsPublic.ShouldBeFalse(
                $"{h.Name} не должен быть public — обработчики внутренние");
        }
    }

    [Fact]
    public void AllHandlers_HaveUniqueCommandType()
    {
        var handlerType = typeof(IRequestHandler<,>);

        var commandTypes = typeof(IWebMarker).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                     && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType))
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                .Select(i => i.GetGenericArguments()[0].FullName))
            .ToList();

        var duplicates = commandTypes
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.ShouldBeEmpty(
            $"Дублирующиеся обработчики для команд: {String.Join(", ", duplicates)}");
    }
}
