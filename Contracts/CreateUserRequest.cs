namespace IdentityService.Contracts;

/// <summary>Запрос администратора на создание пользователя.</summary>
public sealed record CreateUserRequest(
    string Email,
    string? DisplayName,
    string Password,
    IReadOnlyList<UserRole> Roles);
