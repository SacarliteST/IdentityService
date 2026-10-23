using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.RejectUser;

internal sealed class RejectUserValidator : AbstractValidator<RejectUserRequest>
{
    public RejectUserValidator()
    {
        RuleFor(request => request.Reason)
            .Must(reason => reason is null || !String.IsNullOrWhiteSpace(reason))
            .WithMessage("Причина не должна состоять только из пробелов.")
            .MaximumLength(500)
            .WithMessage("Причина не должна превышать 500 символов.");
    }
}
