using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.ListUsers;

internal sealed class GetUsersRequestValidator : AbstractValidator<GetUsersRequest>
{
    private static readonly string[] Roles =
        [RoleNames.Admin, RoleNames.Teacher, RoleNames.Student];

    private static readonly string[] Statuses =
        [UserStatuses.Active, UserStatuses.Blocked];

    public GetUsersRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .When(request => request.Page.HasValue);
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .When(request => request.PageSize.HasValue);
        RuleFor(request => request.Search).MaximumLength(256);
        RuleFor(request => request.Role)
            .Must(role => role is null || Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be Admin, Teacher or Student.");
        RuleFor(request => request.Status)
            .Must(status => status is null || Statuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be Active or Blocked.");
    }
}
