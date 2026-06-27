using System.Security.Cryptography;

namespace IdentityService.Web.Common.Tokens;

/// <summary>
/// Вычисляет SHA-256 хэш raw refresh-токена в формате base64url.
/// В базе данных хранится только хэш — сырое значение никогда не персистируется.
/// </summary>
internal static class TokenHasher
{
    /// <summary>Возвращает base64url(SHA-256(<paramref name="rawToken"/>)).</summary>
    public static string Hash(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(hash);
    }
}
