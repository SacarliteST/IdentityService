namespace IdentityService.Contracts;

/// <summary>Фильтры журнала активности одного пользователя.</summary>
public sealed class GetUserActivityRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? EventType { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
