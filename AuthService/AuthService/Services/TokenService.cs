using AuthService.Entities;
using AuthService.Infrastructure.Authentication;
using AuthService.Interfaces.Services;
using AuthService.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthService.Services;

public sealed class TokenService(IOptions<JwtOptions> options, IRsaKeyProvider rsaKeyProvider) : ITokenService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(options.Value.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimConstants.UserId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimConstants.Email, user.Email)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimConstants.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(ClaimConstants.Permission, permission));
        }

        var signingKey = rsaKeyProvider.GetPrivateSigningKey();
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
        var header = new JwtHeader(signingCredentials);
        if (!string.IsNullOrWhiteSpace(signingKey.KeyId))
        {
            header["kid"] = signingKey.KeyId;
        }

        var payload = new JwtPayload(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            issuedAt: now);
        var tokenDescriptor = new JwtSecurityToken(header, payload);

        return new AccessTokenResult(_tokenHandler.WriteToken(tokenDescriptor), expiresAtUtc);
    }
}
