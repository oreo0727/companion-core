using System.Security.Claims;
using Companion.Core.Constants;

namespace Companion.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user id claim is missing.");
    }

    public static Guid GetRequiredUserProfileId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(CompanionClaimTypes.UserProfileId);

        return Guid.TryParse(value, out var userProfileId)
            ? userProfileId
            : throw new InvalidOperationException("Authenticated user profile claim is missing.");
    }
}
