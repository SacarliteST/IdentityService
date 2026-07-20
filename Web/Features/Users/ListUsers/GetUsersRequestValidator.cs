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
            .WithMessage("Номер страницы должен быть не меньше 1.")
            .When(request => request.Page.HasValue);
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Размер страницы должен быть от 1 до 100.")
            .When(request => request.PageSize.HasValue);
        RuleFor(request => request.Search)
            .MaximumLength(256)
            .WithMessage("Поисковая строка не должна превышать 256 символов.");
        RuleFor(request => request.Role)
            .Must(role => role is null || Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Роль должна иметь значение Admin, Teacher или Student.");
        RuleFor(request => request.Status)
            .Must(status => status is null || Statuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Статус должен иметь значение Active или Blocked.");
    }
}
