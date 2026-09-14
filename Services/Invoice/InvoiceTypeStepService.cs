using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Helpers;
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

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        var query = _invoiceDb.InvoiceTypeSteps.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            query = query.Where(s => s.StepName.Contains(term));
        }

        return await query
            .OrderBy(s => s.InvoiceTypeId)
            .ThenBy(s => s.StepOrder)
            .Select(s => new IdNameDto
            {
                Id = s.Id,
                Name = s.StepName
            })
            .ToListAsync();
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
        await _authLookup.EnsureUsersExistAsync(request.Approvers.Select(a => a.UserId));
        EnsureDistinctApprovers(request.Approvers);

        var existingOrders = await _invoiceDb.InvoiceTypeSteps
            .Where(s => s.InvoiceTypeId == invoiceTypeId && s.IsActive)
            .Select(s => s.StepOrder)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var step = new InvoiceTypeStep
        {
            InvoiceTypeId = invoiceTypeId,
            StepOrder = SequentialOrderHelper.NextAppendOrder(existingOrders, request.StepOrder),
            StepName = request.StepName.Trim(),
            StepRoleTag = NormalizeTag(request.StepRoleTag),
            DepartmentId = request.DepartmentId,
            MaxDurationDays = request.MaxDurationDays,
            IsActive = true,
            CreatedAt = now
        };

        if (request.DepartmentId is null)
        {
            var ordered = request.Approvers
                .OrderBy(a => a.Priority)
                .ThenBy(a => a.UserId)
                .Select(a => new InvoiceTypeStepApprover
                {
                    UserId = a.UserId,
                    IsActive = true,
                    CreatedAt = now
                })
                .ToList();
            SequentialOrderHelper.AssignSequential(ordered, (a, order) => a.Priority = order);
            foreach (var approver in ordered)
            {
                step.Approvers.Add(approver);
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

        var now = DateTime.UtcNow;
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
                if (approver.Id > 0)
                {
                    approver.Priority = -approver.Id;
                }
            }
        }

        step.DepartmentId = request.DepartmentId;

        var siblings = await _invoiceDb.InvoiceTypeSteps
            .Where(s => s.InvoiceTypeId == step.InvoiceTypeId && s.IsActive)
            .ToListAsync();
        var moved = false;
        if (step.IsActive)
        {
            moved = await SequentialOrderHelper.MoveAsync(
                siblings,
                step,
                request.StepOrder,
                s => s.StepOrder,
                (s, order) =>
                {
                    s.StepOrder = order;
                    s.UpdatedAt = now;
                },
                s => s.Id,
                () => _invoiceDb.SaveChangesAsync());
        }

        if (!moved)
        {
            await _invoiceDb.SaveChangesAsync();
        }

        return await GetByIdAsync(id);
    }

    public async Task<InvoiceTypeStepResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var step = await LoadStepAsync(id, asNoTracking: false);
        if (step.IsActive == request.IsActive)
        {
            return await GetByIdAsync(id);
        }

        var now = DateTime.UtcNow;
        step.IsActive = request.IsActive;
        step.UpdatedAt = now;

        if (!request.IsActive)
        {
            step.StepOrder = -step.Id;
            await _invoiceDb.SaveChangesAsync();

            var remaining = await _invoiceDb.InvoiceTypeSteps
                .Where(s => s.InvoiceTypeId == step.InvoiceTypeId && s.IsActive)
                .ToListAsync();
            await SequentialOrderHelper.CompactAsync(
                remaining,
                s => s.StepOrder,
                (s, order) =>
                {
                    s.StepOrder = order;
                    s.UpdatedAt = now;
                },
                s => s.Id,
                () => _invoiceDb.SaveChangesAsync());
        }
        else
        {
            var existingOrders = await _invoiceDb.InvoiceTypeSteps
                .Where(s => s.InvoiceTypeId == step.InvoiceTypeId && s.IsActive && s.Id != step.Id)
                .Select(s => s.StepOrder)
                .ToListAsync();
            step.StepOrder = SequentialOrderHelper.NextAppendOrder(existingOrders, 0);
            await _invoiceDb.SaveChangesAsync();
        }

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var step = await LoadStepAsync(id, asNoTracking: false);
        var now = DateTime.UtcNow;
        var invoiceTypeId = step.InvoiceTypeId;
        step.IsActive = false;
        step.DeletedAt = now;
        step.UpdatedAt = now;
        step.StepOrder = -step.Id;

        foreach (var approver in step.Approvers)
        {
            approver.IsActive = false;
            approver.DeletedAt = now;
            approver.UpdatedAt = now;
            if (approver.Id > 0)
            {
                approver.Priority = -approver.Id;
            }
        }

        await _invoiceDb.SaveChangesAsync();

        var remaining = await _invoiceDb.InvoiceTypeSteps
            .Where(s => s.InvoiceTypeId == invoiceTypeId && s.IsActive)
            .ToListAsync();
        await SequentialOrderHelper.CompactAsync(
            remaining,
            s => s.StepOrder,
            (s, order) =>
            {
                s.StepOrder = order;
                s.UpdatedAt = now;
            },
            s => s.Id,
            () => _invoiceDb.SaveChangesAsync());
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
    }

    private static string? NormalizeTag(string? tag)
    {
        return string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToUpperInvariant();
    }
}
