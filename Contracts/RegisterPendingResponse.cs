namespace IdentityService.Contracts;

/// <summary>Ответ 202 на регистрацию: заявка принята и ждёт решения администратора, токенов нет.</summary>
public sealed record RegisterPendingResponse(string Status, string Email, string Message);
