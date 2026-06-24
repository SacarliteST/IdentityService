using IdentityService.Common.Results;

namespace IdentityService.Host.Common;

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
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(detail: error.Message, statusCode: statusCode, title: error.Code);
    }
}
