using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TimeNator.Api.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? throw new InvalidOperationException("Token has no subject claim."));
}
