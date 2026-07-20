namespace IdentityService.Contracts;

/// <summary>Карточка пользователя для административного контура.</summary>
public sealed record UserDetailsDto(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? BlockedAt,
    string? BlockReason);
