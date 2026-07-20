using IdentityService.Common.Results;

namespace IdentityService.Web.Common;

/// <summary>
/// Расширения для конвертации <see cref="Result"/> / <see cref="Result{T}"/> в HTTP-ответы минимального API.
/// Маппинг <see cref="ErrorType"/> → HTTP-статус:
/// Validation → 422, Unauthorized → 401, NotFound → 404, Conflict → 409, Failure → 500.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToOk(this Result result) =>
        result.IsSuccess ? TypedResults.Ok() : ToProblem(result.Error!);

    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value!) : ToProblem(result.Error!);

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> locationFn) =>
        result.IsSuccess
            ? TypedResults.Created(locationFn(result.Value!), result.Value!)
            : ToProblem(result.Error!);

    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error!);

    public static IResult ToProblem(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [String.Empty] = [error.Message] },
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: error.Code);
        }

        var statusCode = error.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(detail: error.Message, statusCode: statusCode, title: error.Code);
    }
}
