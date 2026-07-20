namespace IdentityService.Contracts;

/// <summary>Постраничный ответ API.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
