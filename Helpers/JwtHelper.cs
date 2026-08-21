using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using InvoiceTrackingSystemBackend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceTrackingSystemBackend.Helpers;

public class JwtHelper : IJwtHelper
{
    private readonly UserDbContext _context;
    private readonly JwtOptions _jwt;

    public JwtHelper(UserDbContext context, IOptions<JwtOptions> jwt)
    {
        _context = context;
        _jwt = jwt.Value;
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime AccessExpiresAt)> GenerateTokensAsync(User user)
    {
        var now = DateTime.UtcNow;
        var roles = await GetUserRolesAsync(user.Id, now);
        var permissions = await GetUserPermissionsAsync(user.Id, now);
        var isManager = await IsDepartmentManagerAsync(user.Id, now);
        var accessExpiresAt = now.AddMinutes(_jwt.AccessTokenExpiryMinutes);
        var accessToken = GenerateAccessToken(user, roles, permissions, isManager, accessExpiresAt);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return (accessToken, refreshToken, accessExpiresAt);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private string GenerateAccessToken(
        User user,
        List<string> roles,
        List<string> permissions,
        bool isManager,
        DateTime expiresAt)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("email_verified", user.IsVerified.ToString().ToLowerInvariant()),
            new("is_manager", isManager.ToString().ToLowerInvariant()),
            new("Roles", string.Join(",", roles)),
            new("Permissions", string.Join(",", permissions))
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<List<string>> GetUserRolesAsync(int userId, DateTime now)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToListAsync();
    }

    private async Task<bool> IsDepartmentManagerAsync(int userId, DateTime now)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive &&
                ur.Role.IsManager);
    }

    private async Task<List<string>> GetUserPermissionsAsync(int userId, DateTime now)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.Permission.IsActive)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }
}
