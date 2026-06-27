using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Domain;
using IdentityService.Web.Features.Auth;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Users.AssignRole;

internal sealed class AssignRoleHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(AssignRoleCommand cmd, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(cmd.UserId));
        }

        if (!await roleManager.RoleExistsAsync(cmd.Role))
        {
            return Result.Fail(AuthErrors.RoleNotFound(cmd.Role));
        }

        await userManager.AddToRoleAsync(user, cmd.Role);
        return Result.Success();
    }
}
