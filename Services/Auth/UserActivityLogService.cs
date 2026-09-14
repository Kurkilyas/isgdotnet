using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Helpers;
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
            IpAddress = ClientIpHelper.Resolve(http),
            UserAgent = http?.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<UserActivityLogDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? userId = null,
        string? description = null,
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
        if(!string.IsNullOrWhiteSpace(description))
        {
            query = query.Where(e => e.Description != null && e.Description.Contains(description));
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

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(string? name = null)
    {
        var rows = await _context.AuthActivityLogs
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Description, e.ActivityType })
            .ToListAsync();

        var items = rows.Select(e => new IdNameDto
        {
            Id = e.Id,
            Name = string.IsNullOrWhiteSpace(e.Description)
                ? e.ActivityType.ToString()
                : e.Description
        });

        return IdNameDto.FilterByName(items, name);
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
