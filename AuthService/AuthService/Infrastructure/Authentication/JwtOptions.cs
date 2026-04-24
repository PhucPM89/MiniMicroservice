namespace AuthService.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public string PrivateKeyPath { get; set; } = "Keys/jwt-private-key.pem";
    public string PublicKeyPath { get; set; } = "Keys/jwt-public-key.pem";
}
