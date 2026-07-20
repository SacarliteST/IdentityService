using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth;

internal sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .WithMessage("Укажите refresh-токен.");
    }
}
