namespace IdentityService.Client;

public class ApiException(int statusCode, ApiProblem? problem)
    : Exception(problem?.Detail ?? problem?.Title ?? "API error")
{
    public int StatusCode { get; } = statusCode;
    public ApiProblem? Problem { get; } = problem;
}

public sealed class UnauthorizedException(ApiProblem? problem)
    : ApiException(401, problem);

public sealed class ConflictException(ApiProblem? problem)
    : ApiException(409, problem);

public sealed class ValidationException(ApiProblem? problem)
    : ApiException(422, problem);
