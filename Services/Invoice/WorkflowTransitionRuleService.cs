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

public class WorkflowTransitionRuleService : IWorkflowTransitionRuleService
{
    private static readonly HashSet<WorkflowActionType> AllowedTriggers =
    [
        WorkflowActionType.MissingDocumentFlagged,
        WorkflowActionType.Auditor1Rejected,
        WorkflowActionType.Auditor2Rejected,
        WorkflowActionType.Rejected,
        WorkflowActionType.Returned
    ];

    private readonly InvoiceDbContext _invoiceDb;

    public WorkflowTransitionRuleService(InvoiceDbContext invoiceDb)
    {
        _invoiceDb = invoiceDb;
    }

    public async Task<PagedResult<WorkflowTransitionRuleResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceTypeId = null,
        WorkflowActionType? triggerAction = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = Query();

        if (invoiceTypeId.HasValue)
        {
            query = query.Where(r => r.TargetStep.InvoiceTypeId == invoiceTypeId.Value);
        }

        if (triggerAction.HasValue)
        {
            query = query.Where(r => r.TriggerAction == triggerAction.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var rules = await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<WorkflowTransitionRuleResponseDto>.Create(
            rules.Select(Map).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        var query = Query();
        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rules = await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        var items = rules.Select(r => new IdNameDto
        {
            Id = r.Id,
            Name = $"{r.SourceStep?.StepName ?? "*"} → {r.TargetStep.StepName} ({r.TriggerAction})"
        });

        return IdNameDto.FilterByName(items, name);
    }

    public async Task<IReadOnlyList<WorkflowTransitionRuleResponseDto>> GetByInvoiceTypeIdAsync(int invoiceTypeId)
    {
        await EnsureInvoiceTypeExistsAsync(invoiceTypeId);

        var rules = await Query()
            .Where(r => r.TargetStep.InvoiceTypeId == invoiceTypeId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        return rules.Select(Map).ToList();
    }

    public async Task<WorkflowTransitionRuleResponseDto> GetByIdAsync(int id)
    {
        return Map(await LoadAsync(id, asNoTracking: true));
    }

    public async Task<WorkflowTransitionRuleResponseDto> CreateAsync(
        int invoiceTypeId,
        CreateWorkflowTransitionRuleRequestDto request)
    {
        await EnsureInvoiceTypeExistsAsync(invoiceTypeId, requireActive: true);
        EnsureAllowedTrigger(request.TriggerAction);

        var (source, target) = await ResolveStepsAsync(
            invoiceTypeId,
            request.SourceStepId,
            request.TargetStepId);

        await EnsureUniqueAsync(invoiceTypeId, request.TriggerAction, source?.Id, excludeId: null);

        var rule = new WorkflowTransitionRule
        {
            TriggerAction = request.TriggerAction,
            SourceStepId = source?.Id,
            TargetStepId = target.Id,
            Priority = request.Priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _invoiceDb.WorkflowTransitionRules.Add(rule);
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(rule.Id);
    }

    public async Task<WorkflowTransitionRuleResponseDto> UpdateAsync(
        int id,
        UpdateWorkflowTransitionRuleRequestDto request)
    {
        var rule = await LoadAsync(id, asNoTracking: false);
        EnsureAllowedTrigger(request.TriggerAction);

        var invoiceTypeId = rule.TargetStep.InvoiceTypeId;
        var (source, target) = await ResolveStepsAsync(
            invoiceTypeId,
            request.SourceStepId,
            request.TargetStepId);

        await EnsureUniqueAsync(invoiceTypeId, request.TriggerAction, source?.Id, excludeId: id);

        rule.TriggerAction = request.TriggerAction;
        rule.SourceStepId = source?.Id;
        rule.TargetStepId = target.Id;
        rule.Priority = request.Priority;
        rule.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<WorkflowTransitionRuleResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var rule = await LoadAsync(id, asNoTracking: false);
        if (rule.IsActive == request.IsActive)
        {
            return Map(rule);
        }

        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var rule = await LoadAsync(id, asNoTracking: false);
        var now = DateTime.UtcNow;
        rule.IsActive = false;
        rule.DeletedAt = now;
        rule.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
    }

    private IQueryable<WorkflowTransitionRule> Query()
    {
        return _invoiceDb.WorkflowTransitionRules
            .AsNoTracking()
            .Include(r => r.SourceStep)
            .Include(r => r.TargetStep);
    }

    private async Task<WorkflowTransitionRule> LoadAsync(int id, bool asNoTracking)
    {
        IQueryable<WorkflowTransitionRule> query = _invoiceDb.WorkflowTransitionRules
            .Include(r => r.SourceStep)
            .Include(r => r.TargetStep);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var rule = await query.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null)
        {
            throw new NotFoundException("Geçiş kuralı bulunamadı.");
        }

        return rule;
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
            throw new BadRequestException("Pasif bir fatura türüne geçiş kuralı eklenemez.");
        }
    }

    private async Task<(InvoiceTypeStep? Source, InvoiceTypeStep Target)> ResolveStepsAsync(
        int invoiceTypeId,
        int? sourceStepId,
        int targetStepId)
    {
        var target = await GetStepAsync(targetStepId);
        if (target.InvoiceTypeId != invoiceTypeId)
        {
            throw new BadRequestException("Hedef adım bu fatura türüne ait değil.");
        }

        InvoiceTypeStep? source = null;
        if (sourceStepId.HasValue)
        {
            source = await GetStepAsync(sourceStepId.Value);
            if (source.InvoiceTypeId != invoiceTypeId)
            {
                throw new BadRequestException("Kaynak adım bu fatura türüne ait değil.");
            }

            if (source.Id == target.Id)
            {
                throw new BadRequestException("Kaynak ve hedef adım aynı olamaz.");
            }
        }

        return (source, target);
    }

    private async Task<InvoiceTypeStep> GetStepAsync(int stepId)
    {
        var step = await _invoiceDb.InvoiceTypeSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stepId);
        if (step is null)
        {
            throw new BadRequestException("Geçerli bir fatura türü adımı seçilmedi.");
        }

        return step;
    }

    private async Task EnsureUniqueAsync(
        int invoiceTypeId,
        WorkflowActionType trigger,
        int? sourceStepId,
        int? excludeId)
    {
        var exists = await _invoiceDb.WorkflowTransitionRules.AnyAsync(r =>
            r.TargetStep.InvoiceTypeId == invoiceTypeId &&
            r.TriggerAction == trigger &&
            r.SourceStepId == sourceStepId &&
            (!excludeId.HasValue || r.Id != excludeId.Value));
        if (exists)
        {
            throw new ConflictException("Bu tetikleyici ve kaynak adım için kural zaten var.");
        }
    }

    private static void EnsureAllowedTrigger(WorkflowActionType trigger)
    {
        if (!AllowedTriggers.Contains(trigger))
        {
            throw new BadRequestException(
                "Geçiş kuralı yalnızca red, iade veya eksik evrak tetikleyicileri için tanımlanır.");
        }
    }

    private static WorkflowTransitionRuleResponseDto Map(WorkflowTransitionRule rule)
    {
        return new WorkflowTransitionRuleResponseDto
        {
            Id = rule.Id,
            InvoiceTypeId = rule.TargetStep.InvoiceTypeId,
            TriggerAction = rule.TriggerAction,
            SourceStepId = rule.SourceStepId,
            SourceStepName = rule.SourceStep?.StepName,
            TargetStepId = rule.TargetStepId,
            TargetStepName = rule.TargetStep.StepName,
            Priority = rule.Priority,
            IsActive = rule.IsActive
        };
    }
}
