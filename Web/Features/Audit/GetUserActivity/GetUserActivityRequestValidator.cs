using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Audit.GetUserActivity;

internal sealed class GetUserActivityRequestValidator : AbstractValidator<GetUserActivityRequest>
{
    public GetUserActivityRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер страницы должен быть не меньше 1.")
            .When(request => request.Page.HasValue);
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Размер страницы должен быть от 1 до 100.")
            .When(request => request.PageSize.HasValue);
        RuleFor(request => request.EventType)
            .Must(value => value is null || AuditEventTypes.All.Contains(value, StringComparer.Ordinal))
            .WithMessage("Указан неизвестный тип события аудита.");
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .WithMessage("Конечная дата должна быть не раньше начальной.")
            .When(request => request.From.HasValue && request.To.HasValue);
    }
}
