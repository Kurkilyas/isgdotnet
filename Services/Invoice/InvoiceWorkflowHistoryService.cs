using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceWorkflowHistoryService : IInvoiceWorkflowHistoryService
{
    private readonly InvoiceDbContext _invoiceDb;

    public InvoiceWorkflowHistoryService(InvoiceDbContext invoiceDb)
    {
        _invoiceDb = invoiceDb;
    }

    public async Task LogAsync(
        int invoiceId,
        WorkflowActionType actionType,
        InvoiceStatus toStatus,
        InvoiceStatus? fromStatus = null,
        int? actorUserId = null,
        string? reason = null)
    {
        _invoiceDb.InvoiceWorkflowHistories.Add(new InvoiceWorkflowHistory
        {
            InvoiceId = invoiceId,
            ActionType = actionType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<PagedResult<InvoiceWorkflowHistoryResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? actorUserId = null,
        WorkflowActionType? actionType = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.InvoiceWorkflowHistories.AsNoTracking();

        if (invoiceId.HasValue)
        {
            query = query.Where(e => e.InvoiceId == invoiceId.Value);
        }

        if (actorUserId.HasValue)
        {
            query = query.Where(e => e.ActorUserId == actorUserId.Value);
        }

        if (actionType.HasValue)
        {
            query = query.Where(e => e.ActionType == actionType.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => Map(e))
            .ToListAsync();

        return PagedResult<InvoiceWorkflowHistoryResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<InvoiceWorkflowHistoryResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _invoiceDb.InvoiceWorkflowHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? null : Map(entity);
    }

    private static InvoiceWorkflowHistoryResponseDto Map(InvoiceWorkflowHistory entity)
    {
        return new InvoiceWorkflowHistoryResponseDto
        {
            Id = entity.Id,
            InvoiceId = entity.InvoiceId,
            ActionType = entity.ActionType,
            FromStatus = entity.FromStatus,
            ToStatus = entity.ToStatus,
            ActorUserId = entity.ActorUserId,
            Reason = entity.Reason,
            CreatedAt = entity.CreatedAt
        };
    }
}
