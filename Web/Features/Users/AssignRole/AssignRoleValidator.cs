using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.AssignRole;

internal sealed class AssignRoleValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleValidator()
    {
        RuleFor(x => x.Role).NotEmpty();
    }
}
