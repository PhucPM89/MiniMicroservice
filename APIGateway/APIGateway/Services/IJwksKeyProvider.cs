using Microsoft.IdentityModel.Tokens;

namespace APIGateway.Services;

public interface IJwksKeyProvider
{
    IReadOnlyCollection<SecurityKey> GetSigningKeys(string? keyId = null);
}
