namespace IdentityService.Contracts;

/// <summary>Данные текущего пользователя для личного кабинета.</summary>
public sealed record MyProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    IReadOnlyList<string> Roles,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

/// <summary>Смена собственного пароля. Все refresh-токены пользователя после смены отзываются.</summary>
public sealed record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Отклонение заявки на регистрацию.</summary>
public sealed record RejectUserRequest(string? Reason);
