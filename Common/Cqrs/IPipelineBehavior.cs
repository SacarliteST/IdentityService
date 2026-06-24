namespace IdentityService.Common.Cqrs;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct);

public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
