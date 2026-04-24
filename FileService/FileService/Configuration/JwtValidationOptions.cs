namespace FileService.Configuration;

public sealed class JwtValidationOptions
{
    public const string SectionName = "JwtValidation";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string JwksUri { get; set; } = string.Empty;
    public int JwksRefreshMinutes { get; set; } = 15;
}
