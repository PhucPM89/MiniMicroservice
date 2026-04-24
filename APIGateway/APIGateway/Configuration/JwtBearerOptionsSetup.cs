using APIGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Constants;

namespace APIGateway.Configuration;

public sealed class JwtBearerOptionsSetup(
    IOptions<GatewayJwtOptions> jwtOptions,
    IJwksKeyProvider jwksKeyProvider) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Value.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, keyId, _) => jwksKeyProvider.GetSigningKeys(keyId),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimConstants.Email,
            RoleClaimType = ClaimConstants.Role
        };
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
