namespace IdentityService.Common.Cqrs;

/// <summary>
/// Обработчик запроса <typeparamref name="TRequest"/>.
/// Регистрируется в DI; вызывается через <see cref="ISender"/>.
/// </summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Выполняет бизнес-логику и возвращает результат.</summary>
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
