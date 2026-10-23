namespace IdentityService.Contracts;

/// <summary>Запрос на самостоятельную регистрацию. ФИО и код приглашения необязательны для обратной совместимости клиентов.</summary>
/// <param name="Email">Email — он же логин.</param>
/// <param name="Password">Пароль.</param>
/// <param name="DisplayName">Отображаемое имя; если не задано, собирается из имени и фамилии.</param>
/// <param name="FirstName">Имя.</param>
/// <param name="LastName">Фамилия.</param>
/// <param name="MiddleName">Отчество.</param>
/// <param name="InviteCode">Код приглашения: с действующим кодом заявка одобряется сразу.</param>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName,
    string? FirstName = null,
    string? LastName = null,
    string? MiddleName = null,
    string? InviteCode = null);
