using IdentityService.Contracts;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Data.Seeding;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var roleName in new[] { RoleNames.Student, RoleNames.Teacher, RoleNames.Admin })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }
    }
}
