using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceTrackingSystemBackend.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class PermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _requiredPermissions;

    public PermissionAttribute(string requiredPermission)
    {
        _requiredPermissions = [requiredPermission];
    }

    public PermissionAttribute(params string[] requiredPermissions)
    {
        _requiredPermissions = requiredPermissions ?? throw new ArgumentNullException(nameof(requiredPermissions));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userPermissions = PermissionClaimReader.GetPermissions(context);

        if (userPermissions.Count == 0)
        {
            context.Result = PermissionClaimReader.Forbid("Yeterli izin bulunamadı.");
            return;
        }

        var hasRequiredPermission = _requiredPermissions.Any(required =>
            userPermissions.Contains(required));

        if (!hasRequiredPermission)
        {
            context.Result = PermissionClaimReader.Forbid(
                $"Gerekli izinler: {string.Join(", ", _requiredPermissions)}");
        }
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAllPermissionsAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _requiredPermissions;

    public RequireAllPermissionsAttribute(params string[] requiredPermissions)
    {
        _requiredPermissions = requiredPermissions ?? throw new ArgumentNullException(nameof(requiredPermissions));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userPermissions = PermissionClaimReader.GetPermissions(context);

        if (userPermissions.Count == 0)
        {
            context.Result = PermissionClaimReader.Forbid("Yeterli izin bulunamadı.");
            return;
        }

        var missing = _requiredPermissions
            .Where(required => !userPermissions.Contains(required))
            .ToList();

        if (missing.Count > 0)
        {
            context.Result = PermissionClaimReader.Forbid($"Eksik izinler: {string.Join(", ", missing)}");
        }
    }
}

internal static class PermissionClaimReader
{
    public static HashSet<string> GetPermissions(AuthorizationFilterContext context)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var claims = context.HttpContext.User.Claims
            .Where(c => c.Type is "Permission" or "Permissions");

        foreach (var claim in claims)
        {
            var parts = claim.Value.Split([",", ", "], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                result.Add(part.Trim());
            }
        }

        return result;
    }

    public static ObjectResult Forbid(string error)
    {
        return new ObjectResult(new
        {
            success = false,
            statusCode = 403,
            error
        })
        {
            StatusCode = 403
        };
    }
}
