using System.Runtime.CompilerServices;

namespace IdentityService.Common.Results;

/// <summary>
/// Фабрика доменных ошибок с автоматическим префиксом вида <c>EntityName.MethodName</c>.
/// <br/>Используется внутри доменных и обработчиков-слайсов:
/// <code>
/// static class Errors : DomainErrors&lt;ApplicationUser&gt; { }
/// return Result.Fail(Errors.NotFound(id));  // код: "ApplicationUser.NotFound"
/// </code>
/// Параметр <paramref name="reason"/> заполняется автоматически через <see cref="CallerMemberNameAttribute"/>.
/// </summary>
public static class DomainErrors<TEntity>
{
    private static string Prefix => typeof(TEntity).Name;

    /// <summary>Ошибка валидации с кодом <c>EntityName.CallerMethod</c>.</summary>
    public static Error Validation(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Validation);

    /// <summary>Ошибка авторизации с кодом <c>EntityName.CallerMethod</c>.</summary>
    public static Error Unauthorized(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Unauthorized);

    /// <summary>Ошибка «не найден» с автоматическим сообщением и кодом <c>EntityName.CallerMethod</c>.</summary>
    public static Error NotFound(object id, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", $"{Prefix} с id '{id}' не найден(а).", ErrorType.NotFound);

    /// <summary>Ошибка конфликта состояния с кодом <c>EntityName.CallerMethod</c>.</summary>
    public static Error Conflict(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Conflict);

    /// <summary>Общая ошибка бизнес-логики с кодом <c>EntityName.CallerMethod</c>.</summary>
    public static Error Failure(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Failure);
}
