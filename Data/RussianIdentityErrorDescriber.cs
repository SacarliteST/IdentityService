using Microsoft.AspNetCore.Identity;

namespace IdentityService.Data;

internal sealed class RussianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Не удалось выполнить операцию с учётной записью.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Данные учётной записи были изменены. Повторите операцию.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "Указан неверный пароль.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "Токен недействителен.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "Этот внешний вход уже связан с другой учётной записью.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"Имя пользователя '{userName}' недопустимо.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"Email '{email}' имеет неверный формат.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Имя пользователя '{userName}' уже используется.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Email '{email}' уже используется.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), $"Название роли '{role}' недопустимо.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), $"Роль '{role}' уже существует.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "Для пользователя уже установлен пароль.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "Блокировка для пользователя не включена.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"Пользователю уже назначена роль '{role}'.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"Пользователю не назначена роль '{role}'.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"Пароль должен содержать не менее {length} символов.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "Пароль должен содержать специальный символ.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "Пароль должен содержать цифру.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "Пароль должен содержать строчную букву.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "Пароль должен содержать заглавную букву.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(
            nameof(PasswordRequiresUniqueChars),
            $"Пароль должен содержать не менее {uniqueChars} различных символов.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "Не удалось применить код восстановления.");

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };
}
