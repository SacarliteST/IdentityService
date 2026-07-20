namespace IdentityService.Contracts;

/// <summary>Запрос на атомарную замену полного набора ролей пользователя.</summary>
public sealed record UpdateUserRolesRequest(IReadOnlyList<UserRole> Roles);
