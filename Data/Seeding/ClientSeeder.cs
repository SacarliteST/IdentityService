using IdentityService.Common.Crypto;
using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Data.Seeding;

/// <summary>
/// Идемпотентно создаёт доверенных сервисных клиентов из секции <c>InitialClients</c> —
/// участников Token Exchange (<c>POST /auth/token/exchange</c>). Секрет хранится только
/// как хэш; уже существующий по <c>ClientId</c> клиент не создаётся повторно и не
/// перезаписывается (смена секрета/аудиторий — отдельная операция, не через сид).
/// </summary>
public static class ClientSeeder
{
    public static async Task SeedClientsAsync(
        AppDbContext db,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        var added = false;

        foreach (var section in configuration.GetSection("InitialClients").GetChildren())
        {
            var clientId = section["ClientId"];
            var clientSecret = section["ClientSecret"];
            var allowedAudiences = section.GetSection("AllowedAudiences").Get<string[]>() ?? [];

            if (String.IsNullOrWhiteSpace(clientId) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                allowedAudiences.Length == 0)
            {
                throw new InvalidOperationException(
                    $"InitialClients:{section.Key} must contain ClientId, ClientSecret and AllowedAudiences.");
            }

            var exists = await db.Clients.AnyAsync(c => c.ClientId == clientId);
            if (exists)
            {
                continue;
            }

            db.Clients.Add(Client.Create(
                clientId,
                TokenHasher.Hash(clientSecret),
                allowedAudiences,
                timeProvider.GetUtcNow()));
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync();
        }
    }
}
