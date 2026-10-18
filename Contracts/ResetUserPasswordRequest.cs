namespace IdentityService.Contracts;

/// <summary>Запрос администратора на установку нового пароля пользователю.</summary>
public sealed record ResetUserPasswordRequest(string NewPassword);
