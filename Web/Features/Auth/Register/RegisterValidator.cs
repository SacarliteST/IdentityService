using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Укажите email.")
            .EmailAddress()
            .WithMessage("Введите корректный email.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Укажите пароль.")
            .MinimumLength(8)
            .WithMessage("Пароль должен содержать не менее 8 символов.");
    }
}
