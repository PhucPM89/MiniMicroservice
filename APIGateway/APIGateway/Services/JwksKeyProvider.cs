using APIGateway.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace APIGateway.Services;

public sealed class JwksKeyProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GatewayJwtOptions> options,
    ILogger<JwksKeyProvider> logger) : IJwksKeyProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyCollection<SecurityKey> _cachedKeys = Array.Empty<SecurityKey>();
    private DateTimeOffset _refreshAfterUtc = DateTimeOffset.MinValue;

    public IReadOnlyCollection<SecurityKey> GetSigningKeys(string? keyId = null)
    {
        var keys = GetSigningKeysInternal(forceRefresh: false);
        var filteredKeys = FilterKeys(keys, keyId);
        if (filteredKeys.Count > 0 || string.IsNullOrWhiteSpace(keyId))
        {
            return filteredKeys;
        }

        keys = GetSigningKeysInternal(forceRefresh: true);
        return FilterKeys(keys, keyId);
    }

    private IReadOnlyCollection<SecurityKey> GetSigningKeysInternal(bool forceRefresh)
    {
        if (!forceRefresh &&
            _cachedKeys.Count > 0 &&
            DateTimeOffset.UtcNow < _refreshAfterUtc)
        {
            return _cachedKeys;
        }

        _refreshLock.Wait();
        try
        {
            if (!forceRefresh &&
                _cachedKeys.Count > 0 &&
                DateTimeOffset.UtcNow < _refreshAfterUtc)
            {
                return _cachedKeys;
            }

            var client = httpClientFactory.CreateClient(nameof(JwksKeyProvider));
            var response = client.GetAsync(options.Value.JwksUri).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var jwksJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var jwks = new JsonWebKeySet(jwksJson);
            var signingKeys = jwks.GetSigningKeys().ToArray();
            if (signingKeys.Length == 0)
            {
                throw new InvalidOperationException("No signing keys were returned by the configured JWKS endpoint.");
            }

            _cachedKeys = signingKeys;
            _refreshAfterUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, options.Value.JwksRefreshMinutes));

            logger.LogInformation(
                "Loaded {KeyCount} signing key(s) from JWKS endpoint '{JwksUri}'.",
                signingKeys.Length,
                options.Value.JwksUri);

            return _cachedKeys;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static IReadOnlyCollection<SecurityKey> FilterKeys(IEnumerable<SecurityKey> keys, string? keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            return keys.ToArray();
        }

        return keys
            .Where(key => string.Equals(key.KeyId, keyId, StringComparison.Ordinal))
            .ToArray();
    }
}
