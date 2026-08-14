using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Shared.Infra.Auth;

public static class ClaimsPrincipalExtensions
{
    // The "this ClaimsPrincipal user" syntax makes this an extension method
    public static Guid ExtractUserId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (Guid.TryParse(userIdStr, out Guid userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("Invalid user token payload.");
    }
}