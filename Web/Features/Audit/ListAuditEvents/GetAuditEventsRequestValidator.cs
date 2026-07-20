using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Audit.ListAuditEvents;

internal sealed class GetAuditEventsRequestValidator : AbstractValidator<GetAuditEventsRequest>
{
    public GetAuditEventsRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .When(request => request.Page.HasValue);
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .When(request => request.PageSize.HasValue);
        RuleFor(request => request.EventType)
            .Must(value => value is null || AuditEventTypes.All.Contains(value, StringComparer.Ordinal))
            .WithMessage("Unknown audit event type.");
        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .When(request => request.From.HasValue && request.To.HasValue);
    }
}
