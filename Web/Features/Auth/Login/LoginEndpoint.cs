using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Auth.Login;

internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Login, Handle)
            .WithName("Login")
            .WithTags("Auth")
            .WithSummary("Аутентификация пользователя")
            .WithDescription("Проверяет учётные данные и возвращает пару токенов.")
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        LoginRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<LoginCommand, Result<TokenResponse>>(
            new LoginCommand(request.Email, request.Password), ct);

        return result.ToOk();
    }
}
