using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Authentication;

public interface IRsaKeyProvider
{
    RsaSecurityKey GetPrivateSigningKey();
    RsaSecurityKey GetPublicSigningKey();
    IEnumerable<SecurityKey> GetPublicSigningKeys(string? keyId = null);
    JwksDocument GetJwksDocument();
}
