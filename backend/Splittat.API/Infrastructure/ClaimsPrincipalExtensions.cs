using System.Security.Claims;

namespace Splittat.API.Infrastructure;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information from JWT tokens
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the user ID (Guid) from JWT claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the authenticated request</param>
    /// <returns>The user's Guid</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID claim is missing or invalid</exception>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID claim not found in token");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format in token");
        }

        return userId;
    }

    /// <summary>
    /// Extracts the user's email from JWT claims
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from the authenticated request</param>
    /// <returns>The user's email address, or null if not found</returns>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }
}
