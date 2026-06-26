using IdentityService.Contracts;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Data.Seeding;

/// <summary>
/// Идемпотентно создаёт роли Student, Teacher, Admin при старте приложения.
/// Вызывается из <c>Program.cs</c> после применения миграций.
/// </summary>
public static class RoleSeeder
{
    /// <summary>Создаёт отсутствующие роли. Уже существующие пропускает.</summary>
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
