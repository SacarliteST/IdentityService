using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Host.Common.SigningKeys;

public interface ISigningKeyProvider
{
    string Kid { get; }

    SigningCredentials GetSigningCredentials();

    JsonWebKey GetPublicJwk();
}
