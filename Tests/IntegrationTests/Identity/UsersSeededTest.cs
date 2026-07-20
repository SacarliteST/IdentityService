using IdentityService.Contracts;
using IdentityService.Data.Seeding;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Identity;

[Collection(nameof(IntegrationTestCollection))]
public sealed class UsersSeededTest(TestApplication testApplication) : ApiTestBase(testApplication)
{
    [DockerFact]
    public async Task SeedUsers_CreatesConfiguredUsersWithRoles_AndIsIdempotent()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var users = new[]
        {
            (Email: $"admin_{suffix}@test.local", Password: "Admin1234", Role: RoleNames.Admin),
            (Email: $"student_{suffix}@test.local", Password: "Student1234", Role: RoleNames.Student),
            (Email: $"teacher_{suffix}@test.local", Password: "Teacher1234", Role: RoleNames.Teacher)
        };

        var values = users
            .SelectMany((user, index) => new Dictionary<string, string?>
            {
                [$"InitialUsers:{index}:Email"] = user.Email,
                [$"InitialUsers:{index}:Password"] = user.Password,
                [$"InitialUsers:{index}:DisplayName"] = $"Seeded {user.Role}",
                [$"InitialUsers:{index}:Role"] = user.Role
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var userManager = Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var timeProvider = Scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await UserSeeder.SeedUsersAsync(userManager, configuration, timeProvider);
        await UserSeeder.SeedUsersAsync(userManager, configuration, timeProvider);

        foreach (var expected in users)
        {
            var user = await userManager.FindByEmailAsync(expected.Email);
            user.ShouldNotBeNull();
            user.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
            user.LastLoginAt.ShouldBeNull();
            (await userManager.IsInRoleAsync(user, expected.Role)).ShouldBeTrue();
            (await userManager.CheckPasswordAsync(user, expected.Password)).ShouldBeTrue();
        }
    }
}
