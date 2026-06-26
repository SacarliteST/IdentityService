namespace IdentityService.Common.Results;

/// <summary>
/// Описание ошибки бизнес-логики. Содержит машиночитаемый код, сообщение и категорию.
/// Передаётся внутри <see cref="Result"/> / <see cref="Result{T}"/> вместо исключений.
/// Для доменных ошибок с автоматическим префиксом используйте <see cref="DomainErrors{TEntity}"/>.
/// </summary>
public record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Ошибка валидации входных данных.</summary>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>Операция не авторизована.</summary>
    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    /// <summary>Запрашиваемый ресурс не найден.</summary>
    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    /// <summary>Конфликт состояния (например, дубликат).</summary>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>Общая ошибка бизнес-логики.</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
