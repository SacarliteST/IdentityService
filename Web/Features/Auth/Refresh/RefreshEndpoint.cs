using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Auth.Refresh;

internal sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Refresh, Handle)
            .WithName("Refresh")
            .WithTags("Auth")
            .WithSummary("Обновление токенов")
            .WithDescription("Ротирует refresh-токен. Повторное использование отозванного токена инициирует отзыв всех сессий.")
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        RefreshRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<RefreshCommand, Result<TokenResponse>>(
            new RefreshCommand(request.RefreshToken), ct);

        return result.ToOk();
    }
}
