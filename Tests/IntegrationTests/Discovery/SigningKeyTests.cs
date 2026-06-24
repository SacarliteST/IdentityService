using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using IdentityService.Host.Common.Options;
using IdentityService.Host.Common.SigningKeys;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace IdentityService.IntegrationTests.Discovery;

public sealed class SigningKeyTests
{
    [Fact]
    public void SignAndValidate_RoundTrip_Succeeds()
    {
        var provider = new RsaSigningKeyProvider(Options.Create(new SigningKeyOptions()));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = provider.GetSigningCredentials()
        });

        var jwk = provider.GetPublicJwk();
        jwk.Kid.ShouldBe(provider.Kid);

        var publicRsa = RSA.Create();
        publicRsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlEncoder.DecodeBytes(jwk.N),
            Exponent = Base64UrlEncoder.DecodeBytes(jwk.E)
        });
        var publicKey = new RsaSecurityKey(publicRsa) { KeyId = jwk.Kid };

        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            IssuerSigningKey = publicKey,
            ValidateLifetime = true
        }, out _);

        principal.ShouldNotBeNull();
    }

    [Fact]
    public void TwoInstances_SameKeyFile_HaveSameKid_AndCrossValidate()
    {
        var keyFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pem");
        try
        {
            var opts = Options.Create(new SigningKeyOptions { KeyFilePath = keyFile });

            using var first = new RsaSigningKeyProvider(opts);
            using var second = new RsaSigningKeyProvider(opts);

            second.Kid.ShouldBe(first.Kid);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateEncodedJwt(new SecurityTokenDescriptor
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = first.GetSigningCredentials()
            });

            var jwk = second.GetPublicJwk();
            var publicRsa = RSA.Create();
            publicRsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(jwk.N),
                Exponent = Base64UrlEncoder.DecodeBytes(jwk.E)
            });
            var publicKey = new RsaSecurityKey(publicRsa) { KeyId = jwk.Kid };

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = "test-issuer",
                ValidAudience = "test-audience",
                IssuerSigningKey = publicKey,
                ValidateLifetime = true
            }, out _);

            principal.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(keyFile);
        }
    }
}
