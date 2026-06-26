namespace IdentityService.Common.Results;

/// <summary>Категория ошибки. Определяет HTTP-статус при маппинге через <see cref="ResultExtensions"/>.</summary>
public enum ErrorType
{
    /// <summary>Ошибка валидации входных данных → HTTP 422.</summary>
    Validation,
    /// <summary>Не авторизован → HTTP 401.</summary>
    Unauthorized,
    /// <summary>Ресурс не найден → HTTP 404.</summary>
    NotFound,
    /// <summary>Конфликт состояния (дубликат и т.п.) → HTTP 409.</summary>
    Conflict,
    /// <summary>Внутренняя ошибка бизнес-логики → HTTP 500.</summary>
    Failure
}
