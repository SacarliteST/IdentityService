namespace IdentityService.Contracts;

/// <summary>Единый ответ API с ошибками валидации полей.</summary>
public sealed record ValidationProblemDetails(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? Instance,
    IReadOnlyDictionary<string, string[]> Errors);
