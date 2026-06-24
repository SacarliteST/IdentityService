namespace IdentityService.Common.Results;

public class Result
{
    public bool IsSuccess { get; private init; }
    public Error? Error { get; private init; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Fail(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; private init; }

    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Fail(Error error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
}
