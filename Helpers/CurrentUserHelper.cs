using System.Security.Claims;
using isgDotnet.Exceptions;

namespace isgDotnet.Helpers;

public static class CurrentUserHelper
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("Oturum bilgisi geçersiz.");
        }

        return userId;
    }

    public static HashSet<string> GetPermissions(ClaimsPrincipal user)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims.Where(c => c.Type is "Permission" or "Permissions"))
        {
            var parts = claim.Value.Split([",", ", "], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                result.Add(part.Trim());
            }
        }

        return result;
    }
}
