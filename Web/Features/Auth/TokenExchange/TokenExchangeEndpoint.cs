using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;
using IdentityService.Web.Common.Auth;

namespace IdentityService.Web.Features.Auth.TokenExchange;

internal sealed class TokenExchangeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.TokenExchange, Handle)
            .WithName("TokenExchange")
            .WithTags("Аутентификация")
            .WithSummary("Обмен токена на аудиторию другого сервиса")
            .WithDescription(
                "Server-to-server: доверенный клиент (Authorization: Basic client_id:client_secret) " +
                "обменивает токен пользователя на новый токен, ограниченный целевой audience. " +
                "Refresh-токен не выдаётся.")
            .Produces<TokenExchangeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<TokenExchangeRequest>>()
            .AllowAnonymous(); // клиент аутентифицируется Basic-заголовком, не JWT bearer middleware
    }

    private static async Task<IResult> Handle(
        TokenExchangeRequest request, HttpContext http, ISender sender, CancellationToken ct)
    {
        var credentials = BasicAuthCredentials.TryParse(http.Request.Headers.Authorization);
        if (credentials is null)
        {
            return ResultExtensions.ToProblem(AuthErrors.InvalidClientCredentials());
        }

        var result = await sender.Send<TokenExchangeCommand, Result<TokenExchangeResponse>>(
            new TokenExchangeCommand(
                credentials.Value.ClientId, credentials.Value.ClientSecret, request.SubjectToken, request.Audience,
                request.SessionId, request.SessionExpiresAt),
            ct);

        return result.ToOk();
    }
}
