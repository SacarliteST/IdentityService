using System.Text;

namespace IdentityService.Web.Common.Auth;

/// <summary>
/// Разбирает заголовок <c>Authorization: Basic base64(client_id:client_secret)</c> —
/// аутентификация клиента в Token Exchange, отдельная от обычного JWT-bearer
/// (у клиента ещё нет пользовательской сессии, он и есть вызывающая сторона).
/// </summary>
internal readonly record struct BasicAuthCredentials(string ClientId, string ClientSecret)
{
    public static BasicAuthCredentials? TryParse(string? authorizationHeader)
    {
        const string scheme = "Basic ";
        if (String.IsNullOrEmpty(authorizationHeader) ||
            !authorizationHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string decoded;
        try
        {
            var raw = Convert.FromBase64String(authorizationHeader[scheme.Length..].Trim());
            decoded = Encoding.UTF8.GetString(raw);
        }
        catch (FormatException)
        {
            return null;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return null;
        }

        var clientId = decoded[..separatorIndex];
        var clientSecret = decoded[(separatorIndex + 1)..];
        if (String.IsNullOrEmpty(clientId) || String.IsNullOrEmpty(clientSecret))
        {
            return null;
        }

        return new BasicAuthCredentials(clientId, clientSecret);
    }
}
