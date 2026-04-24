using Microsoft.IdentityModel.Tokens;

namespace FileService.Services.Authentication;

public interface IJwksKeyProvider
{
    IReadOnlyCollection<SecurityKey> GetSigningKeys(string? keyId = null);
}
