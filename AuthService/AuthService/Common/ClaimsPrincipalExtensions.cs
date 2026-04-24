using Shared.Constants;
using Shared.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthService.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimConstants.UserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException("The access token does not contain a valid user identifier.");
        }

        return userId;
    }
}
