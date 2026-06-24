namespace IdentityService.Common.Cqrs;

public interface ISender
{
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
        where TRequest : IRequest<TResponse>;
}
