using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Login;

internal sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Укажите email.")
            .EmailAddress()
            .WithMessage("Введите корректный email.");
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Укажите пароль.");
    }
}
