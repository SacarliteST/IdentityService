using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Common.Crypto;

/// <summary>
/// Вычисляет SHA-256 хэш секрета/токена в формате base64url. В БД хранится только
/// хэш — сырое значение никогда не персистируется. Используется и для refresh-токенов
/// (<c>Web.Common.Tokens.TokenService</c>), и для секретов сервисных клиентов
/// (<c>Data.Seeding.ClientSeeder</c>, Token Exchange) — общий уровень, доступный обоим.
/// </summary>
public static class TokenHasher
{
    /// <summary>Возвращает base64url(SHA-256(<paramref name="rawValue"/>)) без пакета IdentityModel — только BCL.</summary>
    public static string Hash(string rawValue)
    {
        var bytes = Encoding.UTF8.GetBytes(rawValue);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
