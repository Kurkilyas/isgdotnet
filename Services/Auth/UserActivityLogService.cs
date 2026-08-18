using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Auth;

public class UserActivityLogService : IUserActivityLogService
{
    private readonly UserDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserActivityLogService(UserDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(int? userId, AuthActivityType activityType, string? description = null)
    {
        var http = _httpContextAccessor.HttpContext;

        _context.AuthActivityLogs.Add(new AuthActivityLog
        {
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<UserActivityLogDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? userId = null,
        AuthActivityType? activityType = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.AuthActivityLogs.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        if (activityType.HasValue)
        {
            query = query.Where(e => e.ActivityType == activityType.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new UserActivityLogDto
            {
                Id = e.Id,
                UserId = e.UserId,
                ActivityType = e.ActivityType,
                Description = e.Description,
                IpAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return PagedResult<UserActivityLogDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<UserActivityLogDto?> GetByIdAsync(int id)
    {
        var entity = await _context.AuthActivityLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? null : Map(entity);
    }

    private static UserActivityLogDto Map(AuthActivityLog entity)
    {
        return new UserActivityLogDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            ActivityType = entity.ActivityType,
            Description = entity.Description,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CreatedAt = entity.CreatedAt
        };
    }
}
