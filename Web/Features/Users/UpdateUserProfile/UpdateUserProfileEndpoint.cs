using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Users.Profile, Handle)
            .WithName("UpdateUserProfile")
            .WithTags("Пользователи")
            .WithSummary("Изменение ФИО пользователя")
            .WithDescription(
                "Меняет имя, фамилию и отчество и пересчитывает отображаемое имя. Учебные сервисы получат " +
                "новые значения в следующем выданном токене (claims given_name, family_name, middle_name). " +
                "Доступно только для роли Admin.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<UpdateUserProfileRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateUserProfileRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct)
    {
        var actorUserId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await sender.Send<UpdateUserProfileCommand, Result>(
            new UpdateUserProfileCommand(
                actorUserId, id, request.FirstName, request.LastName, request.MiddleName),
            ct);

        return result.ToNoContent();
    }
}
