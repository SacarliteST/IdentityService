using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Features.Audit.GetUserActivity;
using IdentityService.Web.Features.Audit.ListAuditEvents;

namespace IdentityService.Web.Features.Audit;

internal static class AuditModule
{
    internal static IServiceCollection AddAudit(this IServiceCollection services) =>
        services
            .AddScoped<
                IRequestHandler<ListAuditEventsQuery, Result<PagedResponse<AuditEventDto>>>,
                ListAuditEventsHandler>()
            .AddScoped<
                IRequestHandler<GetUserActivityQuery, Result<PagedResponse<AuditEventDto>>>,
                GetUserActivityHandler>();
}
