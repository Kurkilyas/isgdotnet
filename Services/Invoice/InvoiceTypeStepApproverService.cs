using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
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

        await EnsurePriorityAvailableAsync(invoiceTypeStepId, request.Priority, excludeId: existing?.Id);

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
            existing.Priority = request.Priority;
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
                Priority = request.Priority,
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
        await EnsurePriorityAvailableAsync(approver.InvoiceTypeStepId, request.Priority, excludeId: id);

        approver.Priority = request.Priority;
        approver.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await MapOneAsync(approver);
    }

    public async Task<InvoiceTypeStepApproverResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var approver = await GetRequiredApproverAsync(id);
        if (approver.IsActive == request.IsActive)
        {
            return await MapOneAsync(approver);
        }

        approver.IsActive = request.IsActive;
        approver.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await MapOneAsync(approver);
    }

    public async Task DeleteAsync(int id)
    {
        var approver = await GetRequiredApproverAsync(id);
        var now = DateTime.UtcNow;
        approver.IsActive = false;
        approver.DeletedAt = now;
        approver.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
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

    private async Task EnsurePriorityAvailableAsync(int invoiceTypeStepId, int priority, int? excludeId)
    {
        var exists = await _invoiceDb.InvoiceTypeStepApprovers.AnyAsync(a =>
            a.InvoiceTypeStepId == invoiceTypeStepId &&
            a.Priority == priority &&
            (!excludeId.HasValue || a.Id != excludeId.Value));
        if (exists)
        {
            throw new ConflictException("Bu öncelik numarası bu adımda zaten kullanılıyor.");
        }
    }

    private async Task<InvoiceTypeStepApproverResponseDto> MapOneAsync(InvoiceTypeStepApprover approver)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync([approver.UserId]);
        return InvoiceMasterDataMapper.MapApprover(approver, userNames);
    }
}
