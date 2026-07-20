using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.CreateUser;

internal sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]")
            .WithMessage("Пароль должен содержать заглавную букву.")
            .Matches("[a-z]")
            .WithMessage("Пароль должен содержать строчную букву.")
            .Matches("[0-9]")
            .WithMessage("Пароль должен содержать цифру.");

        RuleFor(request => request.DisplayName)
            .Must(displayName => displayName is null || !String.IsNullOrWhiteSpace(displayName))
            .WithMessage("Имя не должно состоять только из пробелов.")
            .MaximumLength(256);

        RuleFor(request => request.Roles)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(roles => roles.Distinct().Count() == roles.Count)
            .WithMessage("Роли не должны повторяться.");

        RuleForEach(request => request.Roles)
            .IsInEnum();
    }
}
