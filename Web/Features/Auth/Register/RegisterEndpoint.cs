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
            .WithTags("Аутентификация")
            .WithSummary("Регистрация нового пользователя")
            .WithDescription(
                "Создаёт пользователя с ролью Student. Режим Approval (по умолчанию): без кода приглашения заявка " +
                "ждёт одобрения администратора (202, токенов нет), с действующим кодом доступ открыт сразу (201 и пара " +
                "токенов). Режим Open — всегда 201, режим Closed — 409.")
            .Produces<TokenResponse>(StatusCodes.Status201Created)
            .Produces<RegisterPendingResponse>(StatusCodes.Status202Accepted)
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .AllowAnonymous();
    }

    private static async Task<IResult> Handle(
        RegisterRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<RegisterCommand, Result<RegisterResult>>(
            new RegisterCommand(
                request.Email, request.Password, request.DisplayName,
                request.FirstName, request.LastName, request.MiddleName, request.InviteCode),
            ct);
        if (!result.IsSuccess)
        {
            return ResultExtensions.ToProblem(result.Error!);
        }

        var value = result.Value!;
        return value.Tokens is not null
            ? TypedResults.Created(ApiRoutes.Auth.Login, value.Tokens)
            : Results.Json(
                new RegisterPendingResponse(
                    value.Status,
                    value.Email,
                    "Заявка принята. Войти можно после одобрения администратором."),
                statusCode: StatusCodes.Status202Accepted);
    }
}
