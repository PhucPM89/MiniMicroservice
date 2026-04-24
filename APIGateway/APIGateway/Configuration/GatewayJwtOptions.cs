namespace APIGateway.Configuration;

public sealed class GatewayJwtOptions
{
    public const string SectionName = "GatewayJwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string JwksUri { get; set; } = string.Empty;
    public int JwksRefreshMinutes { get; set; } = 15;
}
