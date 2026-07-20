namespace IdentityService.Contracts;

/// <summary>Причина административной блокировки пользователя.</summary>
public sealed record BlockUserRequest(string? Reason);
