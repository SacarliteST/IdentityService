using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.UpdateUserRoles;

internal sealed class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesValidator()
    {
        RuleFor(request => request.Roles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Выберите хотя бы одну роль.")
            .Must(roles => roles.Distinct().Count() == roles.Count)
            .WithMessage("Роли не должны повторяться.");
        RuleForEach(request => request.Roles)
            .IsInEnum()
            .WithMessage("Указана неизвестная роль.");
    }
}
