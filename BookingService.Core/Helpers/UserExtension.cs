using System.Security.Claims;

namespace BookingService.Core.Helpers;
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value;

        if (value is null)
            throw new UnauthorizedAccessException("User id claim not found.");
        
        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Invalid user id.");

        return Guid.Parse(value);
    }
}
