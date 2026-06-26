namespace IdentityService.Common.Cqrs;

/// <summary>
/// Маркерный интерфейс запроса в CQRS-пайплайне.
/// <typeparamref name="TResponse"/> — тип возвращаемого результата.
/// </summary>
public interface IRequest<TResponse>;
