namespace IdentityService.Contracts;

/// <summary>Пользователь в административном списке.</summary>
public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
