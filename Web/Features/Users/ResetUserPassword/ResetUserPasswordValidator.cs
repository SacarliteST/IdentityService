using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.ResetUserPassword;

internal sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithMessage("Укажите новый пароль.")
            .MinimumLength(8)
            .WithMessage("Пароль должен содержать не менее 8 символов.")
            .Matches("[A-Z]")
            .WithMessage("Пароль должен содержать заглавную букву.")
            .Matches("[a-z]")
            .WithMessage("Пароль должен содержать строчную букву.")
            .Matches("[0-9]")
            .WithMessage("Пароль должен содержать цифру.");
    }
}
