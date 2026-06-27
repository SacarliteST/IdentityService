using System.Net.Http.Json;

namespace IdentityService.Client;

public sealed class ErrorDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(
                ClientJson.Default, cancellationToken);
        }
        catch { /* ignore deserialization failures */ }

        throw (int)response.StatusCode switch
        {
            401 => new UnauthorizedException(problem),
            403 => new ForbiddenException(problem),
            409 => new ConflictException(problem),
            422 => new ValidationException(problem),
            _ => new ApiException((int)response.StatusCode, problem)
        };
    }
}
