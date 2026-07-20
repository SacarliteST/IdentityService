using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Укажите email.")
            .EmailAddress()
            .WithMessage("Введите корректный email.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Укажите пароль.")
            .MinimumLength(8)
            .WithMessage("Пароль должен содержать не менее 8 символов.")
            .Matches("[A-Z]")
            .WithMessage("Пароль должен содержать заглавную букву.")
            .Matches("[a-z]")
            .WithMessage("Пароль должен содержать строчную букву.")
            .Matches("[0-9]")
            .WithMessage("Пароль должен содержать цифру.");

        RuleFor(request => request.DisplayName)
            .Must(displayName => displayName is null || !String.IsNullOrWhiteSpace(displayName))
            .WithMessage("Имя не должно состоять только из пробелов.")
            .MaximumLength(256)
            .WithMessage("Имя не должно превышать 256 символов.");

        RuleFor(request => request.Roles)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Укажите роли пользователя.")
            .NotEmpty()
            .WithMessage("Выберите хотя бы одну роль.")
            .Must(roles => roles.Distinct().Count() == roles.Count)
            .WithMessage("Роли не должны повторяться.");

        RuleForEach(request => request.Roles)
            .IsInEnum()
            .WithMessage("Указана неизвестная роль.");
    }
}
