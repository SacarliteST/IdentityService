using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Web.Common;

namespace IdentityService.Web.Features.Users.CreateUser;

internal sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.List, Handle)
            .WithName("CreateUser")
            .WithTags("Users")
            .WithSummary("Административное создание пользователя")
            .WithDescription("Создаёт пользователя с полным набором ролей без выдачи токенов. Только для Admin.")
            .Produces<UserDetailsDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<IResult> Handle(
        CreateUserRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<CreateUserCommand, Result<UserDetailsDto>>(
            new CreateUserCommand(
                request.Email,
                request.DisplayName,
                request.Password,
                request.Roles),
            ct);

        return result.ToCreated(user => $"{ApiRoutes.Users.List}/{user.Id}");
    }
}
