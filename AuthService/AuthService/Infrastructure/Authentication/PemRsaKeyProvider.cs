using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthService.Infrastructure.Authentication;

public sealed class PemRsaKeyProvider(IOptions<JwtOptions> options, IWebHostEnvironment environment) : IRsaKeyProvider
{
    private readonly Lazy<KeyMaterial> _keyMaterial = new(() => LoadKeys(options.Value, environment.ContentRootPath));

    public RsaSecurityKey GetPrivateSigningKey()
    {
        return _keyMaterial.Value.PrivateKey;
    }

    public RsaSecurityKey GetPublicSigningKey()
    {
        return _keyMaterial.Value.PublicKey;
    }

    public IEnumerable<SecurityKey> GetPublicSigningKeys(string? keyId = null)
    {
        var publicKey = _keyMaterial.Value.PublicKey;
        if (!string.IsNullOrWhiteSpace(keyId) &&
            !string.Equals(publicKey.KeyId, keyId, StringComparison.Ordinal))
        {
            return Array.Empty<SecurityKey>();
        }

        return new SecurityKey[] { publicKey };
    }

    public JwksDocument GetJwksDocument()
    {
        return _keyMaterial.Value.JwksDocument;
    }

    private static KeyMaterial LoadKeys(JwtOptions options, string contentRootPath)
    {
        var privatePath = ResolvePath(options.PrivateKeyPath, contentRootPath);
        var publicPath = ResolvePath(options.PublicKeyPath, contentRootPath);

        var privatePem = File.ReadAllText(privatePath);
        var publicPem = File.ReadAllText(publicPath);

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privatePem);

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicPem);

        var privateParameters = privateRsa.ExportParameters(true);
        var publicParameters = publicRsa.ExportParameters(false);

        if (!HaveSamePublicMaterial(privateParameters, publicParameters))
        {
            throw new InvalidOperationException("JWT private key and public key do not belong to the same RSA key pair.");
        }

        var keyId = CreateKeyId(publicParameters);
        var privateKey = new RsaSecurityKey(privateRsa) { KeyId = keyId };
        var publicKey = new RsaSecurityKey(publicRsa) { KeyId = keyId };
        var jwksDocument = new JwksDocument(
            [
                new JwkKeyDocument(
                    KeyType: "RSA",
                    PublicKeyUse: "sig",
                    Algorithm: SecurityAlgorithms.RsaSha256,
                    KeyId: keyId,
                    Modulus: Base64UrlEncoder.Encode(publicParameters.Modulus ?? throw new InvalidOperationException("RSA modulus is missing.")),
                    Exponent: Base64UrlEncoder.Encode(publicParameters.Exponent ?? throw new InvalidOperationException("RSA exponent is missing.")))
            ]);

        return new KeyMaterial(privateKey, publicKey, jwksDocument);
    }

    private static string ResolvePath(string configuredPath, string contentRootPath)
    {
        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRootPath, configuredPath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"JWT RSA key file was not found at '{path}'.", path);
        }

        return path;
    }

    private static bool HaveSamePublicMaterial(RSAParameters privateParameters, RSAParameters publicParameters)
    {
        return privateParameters.Modulus is not null &&
               publicParameters.Modulus is not null &&
               privateParameters.Exponent is not null &&
               publicParameters.Exponent is not null &&
               privateParameters.Modulus.AsSpan().SequenceEqual(publicParameters.Modulus) &&
               privateParameters.Exponent.AsSpan().SequenceEqual(publicParameters.Exponent);
    }

    private static string CreateKeyId(RSAParameters publicParameters)
    {
        var modulus = publicParameters.Modulus ?? throw new InvalidOperationException("RSA modulus is missing.");
        var exponent = publicParameters.Exponent ?? throw new InvalidOperationException("RSA exponent is missing.");

        var bytes = new byte[modulus.Length + exponent.Length];
        Buffer.BlockCopy(modulus, 0, bytes, 0, modulus.Length);
        Buffer.BlockCopy(exponent, 0, bytes, modulus.Length, exponent.Length);

        return Base64UrlEncoder.Encode(SHA256.HashData(bytes));
    }

    private sealed record KeyMaterial(
        RsaSecurityKey PrivateKey,
        RsaSecurityKey PublicKey,
        JwksDocument JwksDocument);
}
