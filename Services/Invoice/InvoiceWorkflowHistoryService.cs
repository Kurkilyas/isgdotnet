using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceWorkflowHistoryService : IInvoiceWorkflowHistoryService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;
    private readonly IInvoiceAccessService _access;

    public InvoiceWorkflowHistoryService(
        InvoiceDbContext invoiceDb,
        AuthReferenceLookup authLookup,
        IInvoiceAccessService access)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
        _access = access;
    }

    public async Task LogAsync(
        int invoiceId,
        WorkflowActionType actionType,
        InvoiceStatus toStatus,
        InvoiceStatus? fromStatus = null,
        int? actorUserId = null,
        string? reason = null)
    {
        var invoiceExists = await _invoiceDb.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.Id == invoiceId);
        if (!invoiceExists)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason is { Length: > 1000 })
        {
            throw new BadRequestException("Gerekçe en fazla 1000 karakter olabilir.");
        }

        _invoiceDb.InvoiceWorkflowHistories.Add(new InvoiceWorkflowHistory
        {
            InvoiceId = invoiceId,
            ActionType = actionType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Reason = trimmedReason,
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

        var access = await _access.ResolveAsync();
        if (invoiceId.HasValue)
        {
            await EnsureCanReadInvoiceAsync(invoiceId.Value, access);
        }
        else if (!access.CanAccessAll && access.InvoiceTypeIds.Count == 0)
        {
            return PagedResult<InvoiceWorkflowHistoryResponseDto>.Create([], 0, page, pageSize);
        }

        var query = _invoiceDb.InvoiceWorkflowHistories.AsNoTracking();

        if (!access.CanAccessAll)
        {
            var typeIds = access.InvoiceTypeIds.ToList();
            query = query.Where(e =>
                e.Invoice.InvoiceTypeId != null &&
                typeIds.Contains(e.Invoice.InvoiceTypeId.Value));
        }

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
        var entities = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await MapManyAsync(entities);
        return PagedResult<InvoiceWorkflowHistoryResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(string? name = null)
    {
        var access = await _access.ResolveAsync();
        if (!access.CanAccessAll && access.InvoiceTypeIds.Count == 0)
        {
            return [];
        }

        var query = _invoiceDb.InvoiceWorkflowHistories.AsNoTracking();
        if (!access.CanAccessAll)
        {
            var typeIds = access.InvoiceTypeIds.ToList();
            query = query.Where(e =>
                e.Invoice.InvoiceTypeId != null &&
                typeIds.Contains(e.Invoice.InvoiceTypeId.Value));
        }

        var rows = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new { e.Id, e.Reason, e.ActionType, e.InvoiceId })
            .ToListAsync();

        var items = rows.Select(e => new IdNameDto
        {
            Id = e.Id,
            Name = string.IsNullOrWhiteSpace(e.Reason)
                ? $"{e.ActionType} #{e.InvoiceId}"
                : e.Reason
        });

        return IdNameDto.FilterByName(items, name);
    }

    public async Task<InvoiceWorkflowHistoryResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _invoiceDb.InvoiceWorkflowHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
        {
            return null;
        }

        var access = await _access.ResolveAsync();
        await EnsureCanReadInvoiceAsync(entity.InvoiceId, access);

        var items = await MapManyAsync([entity]);
        return items[0];
    }

    private async Task EnsureCanReadInvoiceAsync(int invoiceId, InvoiceAccessScope access)
    {
        var invoice = await _invoiceDb.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.InvoiceTypeId })
            .FirstOrDefaultAsync();
        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        access.EnsureCanRead(invoice.InvoiceTypeId);
    }

    private async Task<List<InvoiceWorkflowHistoryResponseDto>> MapManyAsync(
        IReadOnlyList<InvoiceWorkflowHistory> entities)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            entities.Where(e => e.ActorUserId.HasValue).Select(e => e.ActorUserId!.Value));

        return entities.Select(e =>
        {
            string? actorFullName = null;
            if (e.ActorUserId.HasValue)
            {
                userNames.TryGetValue(e.ActorUserId.Value, out actorFullName);
            }

            return new InvoiceWorkflowHistoryResponseDto
            {
                Id = e.Id,
                InvoiceId = e.InvoiceId,
                ActionType = e.ActionType,
                FromStatus = e.FromStatus,
                ToStatus = e.ToStatus,
                ActorUserId = e.ActorUserId,
                ActorUserFullName = actorFullName,
                Reason = e.Reason,
                CreatedAt = e.CreatedAt
            };
        }).ToList();
    }
}
