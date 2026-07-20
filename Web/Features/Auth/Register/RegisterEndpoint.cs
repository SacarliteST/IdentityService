using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Register, Handle)
            .WithName("Register")
            .WithTags("Auth")
            .WithSummary("Регистрация нового пользователя")
            .WithDescription("Создаёт пользователя с ролью Student и возвращает пару токенов.")
            .Produces<TokenResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        RegisterRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<RegisterCommand, Result<TokenResponse>>(
            new RegisterCommand(request.Email, request.Password, request.DisplayName), ct);

        return result.ToCreated(_ => ApiRoutes.Auth.Login);
    }
}
