using System.Security.Cryptography;
using IdentityService.Host.Common.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Host.Common.SigningKeys;

internal sealed class RsaSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly RSA rsa;

    public string Kid { get; }

    public RsaSigningKeyProvider(IOptions<SigningKeyOptions> options)
    {
        var opts = options.Value;

        if (!string.IsNullOrEmpty(opts.PrivateKeyPem))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(opts.PrivateKeyPem.AsSpan());
        }
        else if (!string.IsNullOrEmpty(opts.KeyFilePath) && File.Exists(opts.KeyFilePath))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(opts.KeyFilePath).AsSpan());
        }
        else
        {
            rsa = RSA.Create(2048);
            if (!string.IsNullOrEmpty(opts.KeyFilePath))
            {
                // Persist so tokens survive restarts — TODO: use secret manager / DB for multi-instance prod
                File.WriteAllText(opts.KeyFilePath, rsa.ExportRSAPrivateKeyPem());
            }
        }

        Kid = string.IsNullOrEmpty(opts.Kid) ? ComputeKid(rsa) : opts.Kid;
    }

    public SigningCredentials GetSigningCredentials()
    {
        return new SigningCredentials(
            new RsaSecurityKey(rsa) { KeyId = Kid },
            SecurityAlgorithms.RsaSha256);
    }

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
