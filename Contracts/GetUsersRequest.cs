namespace IdentityService.Contracts;

/// <summary>Параметры постраничного списка пользователей.</summary>
public sealed class GetUsersRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Search { get; init; }

    public string? Role { get; init; }

    public string? Status { get; init; }
}
