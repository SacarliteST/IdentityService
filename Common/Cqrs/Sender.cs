using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Common.Cqrs;

/// <summary>
/// Реализация <see cref="ISender"/>. Строит пайплайн из зарегистрированных
/// <see cref="IPipelineBehavior{TRequest,TResponse}"/> и передаёт управление обработчику.
/// <br/>
/// Порядок: behaviours регистрируются слева направо, разворачиваются через <c>Reverse()</c>,
/// поэтому первый зарегистрированный behaviour выполняется первым (как middleware в ASP.NET Core).
/// </summary>
public sealed class Sender(IServiceProvider sp) : ISender
{
    public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToList();

        RequestHandlerDelegate<TResponse> pipeline = token => handler.Handle(request, token);

        foreach (var behavior in behaviors)
        {
            var captured = pipeline;
            var beh = behavior;
            pipeline = token => beh.Handle(request, captured, token);
        }

        return pipeline(ct);
    }
}
