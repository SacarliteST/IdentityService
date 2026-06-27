namespace IdentityService.Contracts;

/// <summary>Запрос на присвоение роли пользователю (только для Admin).</summary>
public sealed record AssignRoleRequest(string Role);
