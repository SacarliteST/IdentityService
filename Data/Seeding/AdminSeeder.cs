using IdentityService.Contracts;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Data.Seeding;

/// <summary>
/// Создаёт первоначального администратора из конфигурации секции <c>InitialAdmin</c>.
/// Если секция пуста или пользователь уже существует — ничего не делает.
/// Вызывается из <c>Program.cs</c> после применения миграций.
/// </summary>
public static class AdminSeeder
{
    /// <summary>
    /// Создаёт admin-пользователя и назначает роль <c>Admin</c>.
    /// Безопасно вызывать повторно: при наличии пользователя возвращает управление без изменений.
    /// </summary>
    public static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];

        if (String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Admin",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
        }
    }
}
