using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Web.Common.Keys;

/// <summary>
/// Синглтон-реализация <see cref="ISigningKeyProvider"/> на базе RSA.
/// Источник ключа выбирается по приоритету при старте:
/// <list type="number">
///   <item><see cref="SigningKeyOptions.PrivateKeyPem"/> — PEM из конфига/секрета.</item>
///   <item><see cref="SigningKeyOptions.KeyFilePath"/> существует → загружает из файла.</item>
///   <item>Генерирует RSA-2048 и, если задан путь, сохраняет PEM на диск для переживания рестартов.</item>
/// </list>
/// <see cref="Kid"/> вычисляется как base64url(SHA-256(n ‖ e)), что делает его стабильным
/// для одного и того же ключа вне зависимости от источника.
/// </summary>
internal sealed class RsaSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly RSA rsa;

    /// <inheritdoc/>
    public string Kid { get; }

    public RsaSigningKeyProvider(IOptions<SigningKeyOptions> options)
    {
        var opts = options.Value;

        if (!String.IsNullOrEmpty(opts.PrivateKeyPem))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(opts.PrivateKeyPem.AsSpan());
        }
        else if (!String.IsNullOrEmpty(opts.KeyFilePath) && File.Exists(opts.KeyFilePath))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(opts.KeyFilePath).AsSpan());
        }
        else
        {
            rsa = RSA.Create(2048);
            if (!String.IsNullOrEmpty(opts.KeyFilePath))
            {
                // Persist so tokens survive restarts — TODO: use secret manager / DB for multi-instance prod
                File.WriteAllText(opts.KeyFilePath, rsa.ExportRSAPrivateKeyPem());
            }
        }

        Kid = String.IsNullOrEmpty(opts.Kid) ? ComputeKid(rsa) : opts.Kid;
    }

    /// <inheritdoc/>
    public SigningCredentials GetSigningCredentials()
    {
        return new SigningCredentials(
            new RsaSecurityKey(rsa) { KeyId = Kid },
            SecurityAlgorithms.RsaSha256);
    }

    /// <inheritdoc/>
    public JsonWebKey GetPublicJwk()
    {
        var publicParams = rsa.ExportParameters(includePrivateParameters: false);
        using var publicRsa = RSA.Create();
        publicRsa.ImportParameters(publicParams);
        var publicKey = new RsaSecurityKey(publicRsa) { KeyId = Kid };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return jwk;
    }

    public void Dispose() => rsa.Dispose();

    private static string ComputeKid(RSA rsa)
    {
        var p = rsa.ExportParameters(false);
        var mod = p.Modulus ?? [];
        var exp = p.Exponent ?? [];
        var data = new byte[mod.Length + exp.Length];
        mod.CopyTo(data.AsSpan());
        exp.CopyTo(data.AsSpan(mod.Length));
        return Base64UrlEncoder.Encode(SHA256.HashData(data));
    }
}
