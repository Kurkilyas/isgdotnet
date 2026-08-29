using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceTypeStepService : IInvoiceTypeStepService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;

    public InvoiceTypeStepService(InvoiceDbContext invoiceDb, AuthReferenceLookup authLookup)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
    }

    public async Task<IReadOnlyList<InvoiceTypeStepResponseDto>> GetByInvoiceTypeIdAsync(int invoiceTypeId)
    {
        await EnsureInvoiceTypeExistsAsync(invoiceTypeId);

        var steps = await _invoiceDb.InvoiceTypeSteps
            .AsNoTracking()
            .Include(s => s.Approvers)
            .Where(s => s.InvoiceTypeId == invoiceTypeId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        return await MapStepsAsync(steps);
    }

    public async Task<InvoiceTypeStepResponseDto> GetByIdAsync(int id)
    {
        var step = await LoadStepAsync(id, asNoTracking: true);
        return (await MapStepsAsync([step]))[0];
    }

    public async Task<InvoiceTypeStepResponseDto> CreateAsync(int invoiceTypeId, CreateInvoiceTypeStepRequestDto request)
    {
        await EnsureInvoiceTypeExistsAsync(invoiceTypeId, requireActive: true);
        ValidateDepartmentXorApprovers(request.DepartmentId, request.Approvers.Count > 0);
        await _authLookup.EnsureDepartmentExistsAsync(request.DepartmentId);
        await EnsureStepOrderAvailableAsync(invoiceTypeId, request.StepOrder);
        await _authLookup.EnsureUsersExistAsync(request.Approvers.Select(a => a.UserId));
        EnsureDistinctApprovers(request.Approvers);

        var now = DateTime.UtcNow;
        var step = new InvoiceTypeStep
        {
            InvoiceTypeId = invoiceTypeId,
            StepOrder = request.StepOrder,
            StepName = request.StepName.Trim(),
            StepRoleTag = NormalizeTag(request.StepRoleTag),
            DepartmentId = request.DepartmentId,
            MaxDurationDays = request.MaxDurationDays,
            IsActive = true,
            CreatedAt = now
        };

        if (request.DepartmentId is null)
        {
            foreach (var approver in request.Approvers)
            {
                step.Approvers.Add(new InvoiceTypeStepApprover
                {
                    UserId = approver.UserId,
                    Priority = approver.Priority,
                    IsActive = true,
                    CreatedAt = now
                });
            }
        }

        _invoiceDb.InvoiceTypeSteps.Add(step);
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(step.Id);
    }

    public async Task<InvoiceTypeStepResponseDto> UpdateAsync(int id, UpdateInvoiceTypeStepRequestDto request)
    {
        var step = await LoadStepAsync(id, asNoTracking: false);
        ValidateDepartmentXorApprovers(request.DepartmentId, hasApprovers: false);
        await _authLookup.EnsureDepartmentExistsAsync(request.DepartmentId);
        await EnsureStepOrderAvailableAsync(step.InvoiceTypeId, request.StepOrder, excludeStepId: id);

        var now = DateTime.UtcNow;
        step.StepOrder = request.StepOrder;
        step.StepName = request.StepName.Trim();
        step.StepRoleTag = NormalizeTag(request.StepRoleTag);
        step.MaxDurationDays = request.MaxDurationDays;
        step.UpdatedAt = now;

        if (request.DepartmentId.HasValue && step.DepartmentId != request.DepartmentId)
        {
            foreach (var approver in step.Approvers.Where(a => a.DeletedAt is null))
            {
                approver.IsActive = false;
                approver.DeletedAt = now;
                approver.UpdatedAt = now;
            }
        }

        step.DepartmentId = request.DepartmentId;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<InvoiceTypeStepResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var step = await LoadStepAsync(id, asNoTracking: false);
        if (step.IsActive == request.IsActive)
        {
            return await GetByIdAsync(id);
        }

        step.IsActive = request.IsActive;
        step.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var step = await LoadStepAsync(id, asNoTracking: false);
        var now = DateTime.UtcNow;
        step.IsActive = false;
        step.DeletedAt = now;
        step.UpdatedAt = now;
        step.StepOrder = -step.Id;

        foreach (var approver in step.Approvers)
        {
            approver.IsActive = false;
            approver.DeletedAt = now;
            approver.UpdatedAt = now;
        }

        await _invoiceDb.SaveChangesAsync();
    }

    private async Task<InvoiceTypeStep> LoadStepAsync(int id, bool asNoTracking)
    {
        IQueryable<InvoiceTypeStep> query = _invoiceDb.InvoiceTypeSteps.Include(s => s.Approvers);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var step = await query.FirstOrDefaultAsync(s => s.Id == id);
        if (step is null)
        {
            throw new NotFoundException("Fatura türü adımı bulunamadı.");
        }

        return step;
    }

    private async Task EnsureInvoiceTypeExistsAsync(int invoiceTypeId, bool requireActive = false)
    {
        var type = await _invoiceDb.InvoiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == invoiceTypeId);
        if (type is null)
        {
            throw new NotFoundException("Fatura türü bulunamadı.");
        }

        if (requireActive && !type.IsActive)
        {
            throw new BadRequestException("Pasif bir fatura türüne adım eklenemez.");
        }
    }

    private async Task EnsureStepOrderAvailableAsync(int invoiceTypeId, int stepOrder, int? excludeStepId = null)
    {
        var exists = await _invoiceDb.InvoiceTypeSteps.AnyAsync(s =>
            s.InvoiceTypeId == invoiceTypeId &&
            s.StepOrder == stepOrder &&
            (!excludeStepId.HasValue || s.Id != excludeStepId.Value));
        if (exists)
        {
            throw new ConflictException("Bu sıra numarası bu fatura türünde zaten kullanılıyor.");
        }
    }

    private async Task<IReadOnlyList<InvoiceTypeStepResponseDto>> MapStepsAsync(IReadOnlyList<InvoiceTypeStep> steps)
    {
        var departmentIds = steps.Where(s => s.DepartmentId.HasValue).Select(s => s.DepartmentId!.Value);
        var userIds = steps.SelectMany(s => s.Approvers).Select(a => a.UserId);
        var departmentNames = await _authLookup.GetDepartmentNamesAsync(departmentIds);
        var userNames = await _authLookup.GetUserFullNamesAsync(userIds);

        return steps
            .Select(s => InvoiceMasterDataMapper.MapStep(s, departmentNames, userNames))
            .ToList();
    }

    private static void ValidateDepartmentXorApprovers(int? departmentId, bool hasApprovers)
    {
        if (departmentId.HasValue && hasApprovers)
        {
            throw new BadRequestException("Departman havuzu adımına kişiye özel onaylayıcı eklenemez.");
        }
    }

    private static void EnsureDistinctApprovers(IReadOnlyList<CreateInvoiceTypeStepApproverRequestDto> approvers)
    {
        if (approvers.Select(a => a.UserId).Distinct().Count() != approvers.Count)
        {
            throw new BadRequestException("Aynı kullanıcı bir adıma birden fazla kez eklenemez.");
        }

        if (approvers.Select(a => a.Priority).Distinct().Count() != approvers.Count)
        {
            throw new BadRequestException("Onaylayıcı öncelik numaraları benzersiz olmalıdır.");
        }
    }

    private static string? NormalizeTag(string? tag)
    {
        return string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToUpperInvariant();
    }
}
