using System.Text.Json.Serialization;

namespace AuthService.Infrastructure.Authentication;

public sealed record JwksDocument(
    [property: JsonPropertyName("keys")] IReadOnlyCollection<JwkKeyDocument> Keys);

public sealed record JwkKeyDocument(
    [property: JsonPropertyName("kty")] string KeyType,
    [property: JsonPropertyName("use")] string PublicKeyUse,
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("kid")] string KeyId,
    [property: JsonPropertyName("n")] string Modulus,
    [property: JsonPropertyName("e")] string Exponent);
