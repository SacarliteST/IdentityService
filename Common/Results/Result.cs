namespace IdentityService.Common.Results;

/// <summary>
/// Результат операции без возвращаемого значения.
/// Замена исключениям для предсказуемого потока управления:
/// обработчики возвращают <c>Result.Success()</c> или <c>Result.Fail(error)</c>,
/// эндпоинты конвертируют результат в HTTP-ответ через <see cref="ResultExtensions"/>.
/// </summary>
public class Result
{
    /// <summary>Операция завершилась успешно.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Ошибка. Заполнена только при <see cref="IsSuccess"/> == <c>false</c>.</summary>
    public Error? Error { get; private init; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Создаёт успешный результат.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Создаёт неуспешный результат с описанием ошибки.</summary>
    public static Result Fail(Error error) => new(false, error);
}

/// <summary>
/// Результат операции с возвращаемым значением типа <typeparamref name="T"/>.
/// Неявное приведение из <typeparamref name="T"/> создаёт успешный результат,
/// что позволяет писать <c>return value;</c> в обработчиках.
/// </summary>
public sealed class Result<T> : Result
{
    /// <summary>Возвращаемое значение. Заполнено только при <see cref="Result.IsSuccess"/> == <c>true</c>.</summary>
    public T? Value { get; private init; }

    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>Создаёт успешный результат со значением.</summary>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Создаёт неуспешный результат с описанием ошибки.</summary>
    public new static Result<T> Fail(Error error) => new(false, default, error);

    /// <summary>Позволяет писать <c>return value;</c> вместо <c>return Result&lt;T&gt;.Success(value);</c>.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
