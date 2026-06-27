namespace IdentityService.Contracts;

/// <summary>Запрос на аутентификацию пользователя.</summary>
public sealed record LoginRequest(string Email, string Password);
