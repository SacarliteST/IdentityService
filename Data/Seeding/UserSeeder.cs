using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Data.Seeding;

/// <summary>
/// Идемпотентно создаёт начальных пользователей из секции <c>InitialUsers</c>
/// и назначает каждому настроенную роль.
/// </summary>
public static class UserSeeder
{
    public static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        foreach (var section in configuration.GetSection("InitialUsers").GetChildren())
        {
            var email = section["Email"];
            var password = section["Password"];
            var displayName = section["DisplayName"];
            var role = section["Role"];

            if (String.IsNullOrWhiteSpace(email) ||
                String.IsNullOrWhiteSpace(password) ||
                String.IsNullOrWhiteSpace(role))
            {
                throw new InvalidOperationException(
                    $"InitialUsers:{section.Key} must contain Email, Password and Role.");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayName,
                    EmailConfirmed = true,
                    CreatedAt = timeProvider.GetUtcNow()
                };

                EnsureSucceeded(
                    await userManager.CreateAsync(user, password),
                    $"create initial user '{email}'");
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                EnsureSucceeded(
                    await userManager.AddToRoleAsync(user, role),
                    $"assign role '{role}' to initial user '{email}'");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = String.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Failed to {operation}: {errors}");
    }
}
