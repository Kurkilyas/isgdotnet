using InvoiceTrackingSystemBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Helpers;

public static class UserPositionHelper
{
    public static async Task<string?> ResolveAsync(UserDbContext context, int userId)
    {
        var map = await ResolveManyAsync(context, [userId]);
        return map.GetValueOrDefault(userId);
    }

    public static async Task<Dictionary<int, string?>> ResolveManyAsync(
        UserDbContext context,
        IReadOnlyCollection<int> userIds)
    {
        var result = userIds.Distinct().ToDictionary(id => id, _ => (string?)null);
        if (result.Count == 0)
        {
            return result;
        }

        var now = DateTime.UtcNow;
        var rows = await context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                userIds.Contains(ur.UserId) &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive)
            .OrderBy(ur => ur.Role.DisplayName)
            .Select(ur => new { ur.UserId, ur.Role.DisplayName })
            .ToListAsync();

        foreach (var group in rows.GroupBy(r => r.UserId))
        {
            result[group.Key] = string.Join(", ", group.Select(r => r.DisplayName));
        }

        return result;
    }
}
