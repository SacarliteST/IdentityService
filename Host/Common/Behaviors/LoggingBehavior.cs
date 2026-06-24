using System.Diagnostics;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;

namespace IdentityService.Host.Common.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(ct);
            sw.Stop();
            var isSuccess = response is not Result r || r.IsSuccess;
            logger.LogInformation("{Request} handled in {Elapsed}ms IsSuccess={Success}",
                name, sw.ElapsedMilliseconds, isSuccess);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{Request} threw after {Elapsed}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
