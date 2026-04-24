using Microsoft.IdentityModel.Tokens;

namespace TransactionService.Services.Authentication;

public interface IJwksKeyProvider
{
    IReadOnlyCollection<SecurityKey> GetSigningKeys(string? keyId = null);
}
