using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceTypeStepApproverService : IInvoiceTypeStepApproverService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;

    public InvoiceTypeStepApproverService(InvoiceDbContext invoiceDb, AuthReferenceLookup authLookup)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        var query = _invoiceDb.InvoiceTypeStepApprovers.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        var rows = await query
            .OrderBy(a => a.InvoiceTypeStepId)
            .ThenBy(a => a.Priority)
            .Select(a => new { a.Id, a.UserId })
            .ToListAsync();

        var userNames = await _authLookup.GetUserFullNamesAsync(rows.Select(r => r.UserId));
        var items = rows.Select(r => new IdNameDto
        {
            Id = r.Id,
            Name = userNames.GetValueOrDefault(r.UserId) ?? $"#{r.UserId}"
        });

        return IdNameDto.FilterByName(items, name);
    }

    public async Task<IReadOnlyList<InvoiceTypeStepApproverResponseDto>> GetByStepIdAsync(int invoiceTypeStepId)
    {
        await GetRequiredStepAsync(invoiceTypeStepId);

        var approvers = await _invoiceDb.InvoiceTypeStepApprovers
            .AsNoTracking()
            .Where(a => a.InvoiceTypeStepId == invoiceTypeStepId)
            .OrderBy(a => a.Priority)
            .ToListAsync();

        var userNames = await _authLookup.GetUserFullNamesAsync(approvers.Select(a => a.UserId));
        return approvers.Select(a => InvoiceMasterDataMapper.MapApprover(a, userNames)).ToList();
    }

    public async Task<InvoiceTypeStepApproverResponseDto> CreateAsync(
        int invoiceTypeStepId,
        CreateInvoiceTypeStepApproverRequestDto request)
    {
        var step = await GetRequiredStepAsync(invoiceTypeStepId);
        if (step.DepartmentId.HasValue)
        {
            throw new BadRequestException("Departman havuzu adımına kişiye özel onaylayıcı eklenemez.");
        }

        if (!step.IsActive)
        {
            throw new BadRequestException("Pasif bir adıma onaylayıcı eklenemez.");
        }

        await _authLookup.EnsureUserExistsAsync(request.UserId);

        var existing = await _invoiceDb.InvoiceTypeStepApprovers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.InvoiceTypeStepId == invoiceTypeStepId &&
                a.UserId == request.UserId);

        var existingOrders = await _invoiceDb.InvoiceTypeStepApprovers
            .Where(a =>
                a.InvoiceTypeStepId == invoiceTypeStepId &&
                a.IsActive &&
                (existing == null || a.Id != existing.Id))
            .Select(a => a.Priority)
            .ToListAsync();
        var priority = SequentialOrderHelper.NextAppendOrder(existingOrders, request.Priority);

        var now = DateTime.UtcNow;
        InvoiceTypeStepApprover approver;
        if (existing is not null)
        {
            if (existing.DeletedAt is null && existing.IsActive)
            {
                throw new ConflictException("Bu kullanıcı bu adıma zaten onaylayıcı olarak eklenmiş.");
            }

            existing.DeletedAt = null;
            existing.IsActive = true;
            existing.Priority = priority;
            existing.UpdatedAt = now;
            existing.CreatedAt ??= now;
            approver = existing;
        }
        else
        {
            approver = new InvoiceTypeStepApprover
            {
                InvoiceTypeStepId = invoiceTypeStepId,
                UserId = request.UserId,
                Priority = priority,
                IsActive = true,
                CreatedAt = now
            };
            _invoiceDb.InvoiceTypeStepApprovers.Add(approver);
        }

        await _invoiceDb.SaveChangesAsync();
        return await MapOneAsync(approver);
    }

    public async Task<InvoiceTypeStepApproverResponseDto> UpdateAsync(int id, UpdateInvoiceTypeStepApproverRequestDto request)
    {
        var approver = await GetRequiredApproverAsync(id);
        var siblings = await _invoiceDb.InvoiceTypeStepApprovers
            .Where(a => a.InvoiceTypeStepId == approver.InvoiceTypeStepId && a.IsActive)
            .ToListAsync();

        if (approver.IsActive)
        {
            var now = DateTime.UtcNow;
            await SequentialOrderHelper.MoveAsync(
                siblings,
                approver,
                request.Priority,
                a => a.Priority,
                (a, order) =>
                {
                    a.Priority = order;
                    a.UpdatedAt = now;
                },
                a => a.Id,
                () => _invoiceDb.SaveChangesAsync());
        }

        return await MapOneAsync(approver);
    }

    public async Task<InvoiceTypeStepApproverResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var approver = await GetRequiredApproverAsync(id);
        if (approver.IsActive == request.IsActive)
        {
            return await MapOneAsync(approver);
        }

        var now = DateTime.UtcNow;
        approver.IsActive = request.IsActive;
        approver.UpdatedAt = now;

        if (!request.IsActive)
        {
            approver.Priority = -approver.Id;
            await _invoiceDb.SaveChangesAsync();

            var remaining = await _invoiceDb.InvoiceTypeStepApprovers
                .Where(a => a.InvoiceTypeStepId == approver.InvoiceTypeStepId && a.IsActive)
                .ToListAsync();
            await SequentialOrderHelper.CompactAsync(
                remaining,
                a => a.Priority,
                (a, order) =>
                {
                    a.Priority = order;
                    a.UpdatedAt = now;
                },
                a => a.Id,
                () => _invoiceDb.SaveChangesAsync());
        }
        else
        {
            var existingOrders = await _invoiceDb.InvoiceTypeStepApprovers
                .Where(a => a.InvoiceTypeStepId == approver.InvoiceTypeStepId && a.IsActive && a.Id != approver.Id)
                .Select(a => a.Priority)
                .ToListAsync();
            approver.Priority = SequentialOrderHelper.NextAppendOrder(existingOrders, 0);
            await _invoiceDb.SaveChangesAsync();
        }

        return await MapOneAsync(approver);
    }

    public async Task DeleteAsync(int id)
    {
        var approver = await GetRequiredApproverAsync(id);
        var stepId = approver.InvoiceTypeStepId;
        var now = DateTime.UtcNow;
        approver.IsActive = false;
        approver.DeletedAt = now;
        approver.UpdatedAt = now;
        approver.Priority = -approver.Id;
        await _invoiceDb.SaveChangesAsync();

        var remaining = await _invoiceDb.InvoiceTypeStepApprovers
            .Where(a => a.InvoiceTypeStepId == stepId && a.IsActive)
            .ToListAsync();
        await SequentialOrderHelper.CompactAsync(
            remaining,
            a => a.Priority,
            (a, order) =>
            {
                a.Priority = order;
                a.UpdatedAt = now;
            },
            a => a.Id,
            () => _invoiceDb.SaveChangesAsync());
    }

    private async Task<InvoiceTypeStep> GetRequiredStepAsync(int invoiceTypeStepId)
    {
        var step = await _invoiceDb.InvoiceTypeSteps.FirstOrDefaultAsync(s => s.Id == invoiceTypeStepId);
        if (step is null)
        {
            throw new NotFoundException("Fatura türü adımı bulunamadı.");
        }

        return step;
    }

    private async Task<InvoiceTypeStepApprover> GetRequiredApproverAsync(int id)
    {
        var approver = await _invoiceDb.InvoiceTypeStepApprovers.FirstOrDefaultAsync(a => a.Id == id);
        if (approver is null)
        {
            throw new NotFoundException("Onaylayıcı bulunamadı.");
        }

        return approver;
    }

    private async Task<InvoiceTypeStepApproverResponseDto> MapOneAsync(InvoiceTypeStepApprover approver)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync([approver.UserId]);
        return InvoiceMasterDataMapper.MapApprover(approver, userNames);
    }
}
