namespace IdentityService.Contracts;

/// <summary>Запрос на регистрацию нового пользователя.</summary>
public sealed record RegisterRequest(string Email, string Password, string? DisplayName);
