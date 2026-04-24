using AuthService.Entities;
using AuthService.Models;

namespace AuthService.Interfaces.Services;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions);
}
