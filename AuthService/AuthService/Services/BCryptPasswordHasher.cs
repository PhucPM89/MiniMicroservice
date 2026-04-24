using AuthService.Interfaces.Services;
using BCrypt.Net;

namespace AuthService.Services;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 8;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
    }
}