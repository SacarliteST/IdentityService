using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.BlockUser;

internal sealed class BlockUserValidator : AbstractValidator<BlockUserRequest>
{
    public BlockUserValidator()
    {
        RuleFor(request => request.Reason)
            .Must(reason => reason is null || !String.IsNullOrWhiteSpace(reason))
            .WithMessage("Причина не должна состоять только из пробелов.")
            .MaximumLength(500);
    }
}
