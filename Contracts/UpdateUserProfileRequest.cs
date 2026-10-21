namespace IdentityService.Contracts;

/// <summary>Запрос администратора на изменение ФИО пользователя. Хотя бы имя или фамилия должны быть заполнены.</summary>
public sealed record UpdateUserProfileRequest(string? FirstName, string? LastName, string? MiddleName);
