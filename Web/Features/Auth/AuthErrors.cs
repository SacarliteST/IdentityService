using IdentityService.Common.Results;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Auth;

/// <summary>Доменные ошибки слайсов аутентификации.</summary>
internal static class AuthErrors
{
    /// <summary>Email уже занят другим пользователем. 409 Conflict.</summary>
    public static Error EmailTaken() =>
        DomainErrors<ApplicationUser>.Conflict("Email уже используется.", nameof(EmailTaken));

    /// <summary>Неверный email или пароль. 401 Unauthorized. Не раскрывает, что именно неверно.</summary>
    public static Error InvalidCredentials() =>
        DomainErrors<ApplicationUser>.Unauthorized("Неверный email или пароль.", nameof(InvalidCredentials));

    /// <summary>Refresh-токен недействителен, истёк или уже был использован. 401 Unauthorized.</summary>
    public static Error InvalidRefreshToken() =>
        DomainErrors<ApplicationUser>.Unauthorized("Refresh-токен недействителен.", nameof(InvalidRefreshToken));

    /// <summary>Пользователь с заданным id не найден. 404 NotFound.</summary>
    public static Error UserNotFound(object id) =>
        DomainErrors<ApplicationUser>.NotFound(id, nameof(UserNotFound));

    /// <summary>Роль не существует. 409 Conflict.</summary>
    public static Error RoleNotFound(string role) =>
        DomainErrors<ApplicationUser>.Conflict($"Роль '{role}' не существует.", nameof(RoleNotFound));

    public static Error CannotRemoveOwnAdminRole() =>
        DomainErrors<ApplicationUser>.Conflict(
            "Нельзя снять роль Admin у собственной учётной записи.",
            nameof(CannotRemoveOwnAdminRole));

    public static Error CannotRemoveLastAdminRole() =>
        DomainErrors<ApplicationUser>.Conflict(
            "Нельзя снять роль у последнего активного администратора.",
            nameof(CannotRemoveLastAdminRole));

    /// <summary>
    /// Преобразует <see cref="IdentityResult"/> в ошибку:
    /// дубль email → EmailTaken (409); прочее → Validation (422).
    /// </summary>
    public static Error FromIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var list = errors.ToList();
        if (list.Exists(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return EmailTaken();
        }

        var message = String.Join("; ", list.Select(e => e.Description));
        return Error.Validation("ApplicationUser.Validation", message);
    }
}
