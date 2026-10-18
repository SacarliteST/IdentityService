using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.ResetUserPassword;

internal sealed class ResetUserPasswordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Users.Password, Handle)
            .WithName("ResetUserPassword")
            .WithTags("Пользователи")
            .WithSummary("Сброс пароля пользователя")
            .WithDescription(
                "Устанавливает новый пароль и отзывает все активные refresh-токены пользователя. " +
                "Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<ResetUserPasswordRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        ResetUserPasswordRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<ResetUserPasswordCommand, Result>(
            new ResetUserPasswordCommand(actorUserId, id, request.NewPassword),
            ct);

        return result.ToNoContent();
    }
}
