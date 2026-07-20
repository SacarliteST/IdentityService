using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.UpdateUserRoles;

internal sealed class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesValidator()
    {
        RuleFor(request => request.Roles)
            .NotEmpty()
            .Must(roles => roles.Distinct().Count() == roles.Count)
            .WithMessage("Roles must not contain duplicates.");
        RuleForEach(request => request.Roles).IsInEnum();
    }
}
