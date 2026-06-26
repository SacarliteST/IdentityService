namespace IdentityService.Common.Cqrs;

/// <summary>
/// Точка входа в CQRS-пайплайн. Инжектируется в эндпоинты вместо прямых зависимостей на обработчики.
/// Реализация <see cref="Sender"/> строит пайплайн из зарегистрированных <see cref="IPipelineBehavior{TRequest,TResponse}"/>
/// и передаёт управление нужному <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// </summary>
public interface ISender
{
    /// <summary>Отправляет запрос через пайплайн и возвращает результат.</summary>
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        where TRequest : IRequest<TResponse>;
}
