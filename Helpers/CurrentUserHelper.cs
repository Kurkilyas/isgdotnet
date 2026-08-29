using System.Security.Claims;
using InvoiceTrackingSystemBackend.Exceptions;

namespace InvoiceTrackingSystemBackend.Helpers;

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
}
