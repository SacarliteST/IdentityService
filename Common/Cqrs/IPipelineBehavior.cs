namespace IdentityService.Common.Cqrs;

/// <summary>Делегат, представляющий следующий шаг пайплайна (или сам обработчик).</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct);

/// <summary>
/// Middleware пайплайна CQRS. Реализации оборачивают вызов <paramref name="next"/>
/// для сквозной логики: логирование, валидация, трассировка и т.п.
/// Порядок выполнения — обратный порядку регистрации в DI.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Вызывает следующий шаг через <paramref name="next"/> и может перехватывать результат.</summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
