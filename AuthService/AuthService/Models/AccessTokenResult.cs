namespace AuthService.Models;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
