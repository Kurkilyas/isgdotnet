using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceActivityLogService : IInvoiceActivityLogService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoiceActivityLogService(
        InvoiceDbContext invoiceDb,
        AuthReferenceLookup authLookup,
        IHttpContextAccessor httpContextAccessor)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        int invoiceId,
        InvoiceActivityType activityType,
        int? userId,
        int? workflowStepId = null,
        string? description = null)
    {
        var invoiceExists = await _invoiceDb.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.Id == invoiceId);
        if (!invoiceExists)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        if (workflowStepId.HasValue)
        {
            var stepExists = await _invoiceDb.InvoiceWorkflowSteps
                .AsNoTracking()
                .AnyAsync(s => s.Id == workflowStepId.Value && s.InvoiceId == invoiceId);
            if (!stepExists)
            {
                throw new BadRequestException("Geçerli bir iş akışı adımı seçilmedi.");
            }
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription is { Length: > 500 })
        {
            throw new BadRequestException("Açıklama en fazla 500 karakter olabilir.");
        }

        _invoiceDb.InvoiceActivityLogs.Add(new InvoiceActivityLog
        {
            InvoiceId = invoiceId,
            WorkflowStepId = workflowStepId,
            UserId = userId,
            ActivityType = activityType,
            Description = trimmedDescription,
            IpAddress = ClientIpHelper.Resolve(_httpContextAccessor.HttpContext),
            CreatedAt = DateTime.UtcNow
        });

        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<PagedResult<InvoiceActivityLogResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? userId = null,
        InvoiceActivityType? activityType = null,
        string? description = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.InvoiceActivityLogs.AsNoTracking();

        if (invoiceId.HasValue)
        {
            query = query.Where(e => e.InvoiceId == invoiceId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        if (activityType.HasValue)
        {
            query = query.Where(e => e.ActivityType == activityType.Value);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var term = description.Trim();
            query = query.Where(e => e.Description != null && e.Description.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var entities = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await MapManyAsync(entities);
        return PagedResult<InvoiceActivityLogResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<InvoiceActivityLogResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _invoiceDb.InvoiceActivityLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
        {
            return null;
        }

        var items = await MapManyAsync([entity]);
        return items[0];
    }

    private async Task<List<InvoiceActivityLogResponseDto>> MapManyAsync(IReadOnlyList<InvoiceActivityLog> entities)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            entities.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value));

        return entities.Select(e =>
        {
            string? userFullName = null;
            if (e.UserId.HasValue)
            {
                userNames.TryGetValue(e.UserId.Value, out userFullName);
            }

            return new InvoiceActivityLogResponseDto
            {
                Id = e.Id,
                InvoiceId = e.InvoiceId,
                WorkflowStepId = e.WorkflowStepId,
                UserId = e.UserId,
                UserFullName = userFullName,
                ActivityType = e.ActivityType,
                Description = e.Description,
                IpAddress = e.IpAddress,
                CreatedAt = e.CreatedAt
            };
        }).ToList();
    }
}
