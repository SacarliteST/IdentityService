using FluentValidation;
using IdentityService.Domain;

namespace IdentityService.Web.Features.Users;

internal static class PersonNameValidation
{
    /// <summary>Часть ФИО: необязательна, но не из одних пробелов и не длиннее <see cref="PersonName.MaxPartLength"/>.</summary>
    public static IRuleBuilderOptions<T, string?> ValidPersonNamePart<T>(
        this IRuleBuilder<T, string?> rule,
        string label) =>
        rule
            .Must(value => value is null || !String.IsNullOrWhiteSpace(value))
            .WithMessage($"{label} не должно состоять только из пробелов.")
            .Must(value => value is null || value.Trim().Length <= PersonName.MaxPartLength)
            .WithMessage($"{label} не должно превышать {PersonName.MaxPartLength} символов.");
}
