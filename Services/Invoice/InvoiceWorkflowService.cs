using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = InvoiceTrackingSystemBackend.Entities.Invoice.Invoice;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceWorkflowService : IInvoiceWorkflowService
{
    private const string ResultApproved = "APPROVED";
    private const string ResultRejected = "REJECTED";
    private const string ResultReturned = "RETURNED";
    private const string ResultMissingDocument = "MISSING_DOCUMENT";
    private const string ApproverPermission = "InvoiceApprover";
    private const string ActForbiddenMessage = "Bu adımı onaylama yetkiniz yok.";
    private const string RuleMissingMessage = "Bu adım için kural tanımlı değil.";
    private const string ConcurrentMessage = "Bu adım başka biri tarafından işlendi.";

    private readonly InvoiceDbContext _invoiceDb;
    private readonly UserDbContext _userDb;
    private readonly IInvoiceWorkflowHistoryService _history;
    private readonly IInvoiceNotificationLogService _notifications;
    private readonly IEmailService _email;

    public InvoiceWorkflowService(
        InvoiceDbContext invoiceDb,
        UserDbContext userDb,
        IInvoiceWorkflowHistoryService history,
        IInvoiceNotificationLogService notifications,
        IEmailService email)
    {
        _invoiceDb = invoiceDb;
        _userDb = userDb;
        _history = history;
        _notifications = notifications;
        _email = email;
    }

    public async Task EnsureTypeHasActiveStepsAsync(int invoiceTypeId)
    {
        var first = await QueryActiveTypeSteps(invoiceTypeId).FirstOrDefaultAsync();
        if (first is null)
        {
            throw new BadRequestException("Bu fatura türünün aktif adımı yok.");
        }

        if (!first.DepartmentId.HasValue && !first.Approvers.Any(a => a.IsActive))
        {
            throw new BadRequestException("İlk adımda onaylayıcı tanımlı değil.");
        }
    }

    public async Task StartAsync(int invoiceId, int? actorUserId)
    {
        var invoice = await GetRequiredInvoiceAsync(invoiceId);
        if (!invoice.InvoiceTypeId.HasValue)
        {
            return;
        }

        var hasSteps = await _invoiceDb.InvoiceWorkflowSteps.AnyAsync(s => s.InvoiceId == invoiceId);
        if (hasSteps)
        {
            return;
        }

        await EnsureTypeHasActiveStepsAsync(invoice.InvoiceTypeId.Value);
        var typeStep = await QueryActiveTypeSteps(invoice.InvoiceTypeId.Value).FirstAsync();
        var fromStatus = invoice.CurrentStatus;
        var opened = await OpenStepAsync(invoice, typeStep, DateTime.UtcNow);
        ApplyInvoiceState(invoice, opened.Step, typeStep);
        invoice.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        await _history.LogAsync(
            invoice.Id,
            WorkflowActionType.Assigned,
            invoice.CurrentStatus,
            fromStatus,
            actorUserId,
            "İş akışı başlatıldı.");

        await NotifyAsync(
            invoice,
            opened,
            EmailType.InvoiceAssigned,
            NotificationType.Assigned,
            actorUserId,
            reason: null);
    }

    public async Task<int> RequireOpenStepForActorAsync(int invoiceId, int actorUserId)
    {
        await GetRequiredInvoiceAsync(invoiceId);
        var step = await _invoiceDb.InvoiceWorkflowSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.InvoiceId == invoiceId && s.CompletedAt == null);
        if (step is null)
        {
            throw new BadRequestException("Açık iş akışı adımı yok.");
        }

        await EnsureCanActAsync(step, actorUserId);
        return step.Id;
    }

    public async Task ApproveAsync(int invoiceId, int actorUserId, ApproveStepRequestDto request)
    {
        var note = TrimToNull(request.Note, 500);
        await TransitionForwardAsync(
            invoiceId,
            request.StepId,
            actorUserId,
            ResultApproved,
            note,
            WorkflowActionType.Approved);
    }

    public async Task RejectAsync(int invoiceId, int actorUserId, RejectStepRequestDto request)
    {
        var reason = RequiredReason(request.Reason);
        var typeStepTag = await _invoiceDb.InvoiceWorkflowSteps
            .AsNoTracking()
            .Where(s => s.Id == request.StepId && s.InvoiceId == invoiceId)
            .Select(s => s.InvoiceTypeStep.StepRoleTag)
            .FirstOrDefaultAsync();
        var trigger = RejectTriggerFromTag(typeStepTag);
        await TransitionByRuleAsync(
            invoiceId,
            request.StepId,
            actorUserId,
            ResultRejected,
            reason,
            trigger,
            EmailType.InvoiceRejected,
            NotificationType.Rejected);
    }

    public async Task ReturnAsync(int invoiceId, int actorUserId, ReturnStepRequestDto request)
    {
        var reason = RequiredReason(request.Reason);
        await TransitionByRuleAsync(
            invoiceId,
            request.StepId,
            actorUserId,
            ResultReturned,
            reason,
            WorkflowActionType.Returned,
            EmailType.InvoiceReturned,
            NotificationType.Returned);
    }

    public async Task FlagMissingDocumentAsync(int invoiceId, int actorUserId, FlagMissingDocumentRequestDto request)
    {
        var reason = RequiredReason(request.Reason);
        await TransitionByRuleAsync(
            invoiceId,
            request.StepId,
            actorUserId,
            ResultMissingDocument,
            reason,
            WorkflowActionType.MissingDocumentFlagged,
            EmailType.InvoiceMissingDocument,
            NotificationType.MissingDocument);
    }

    private async Task TransitionForwardAsync(
        int invoiceId,
        int stepId,
        int actorUserId,
        string result,
        string? note,
        WorkflowActionType actionType)
    {
        var (invoice, step, typeStep) = await LoadOpenStepAsync(invoiceId, stepId);
        await EnsureCanActAsync(step, actorUserId);

        var fromStatus = invoice.CurrentStatus;
        var now = DateTime.UtcNow;
        var nextTypeStep = await _invoiceDb.InvoiceTypeSteps
            .Include(s => s.Approvers)
            .Where(s =>
                s.InvoiceTypeId == typeStep.InvoiceTypeId &&
                s.IsActive &&
                s.StepOrder > typeStep.StepOrder)
            .OrderBy(s => s.StepOrder)
            .FirstOrDefaultAsync();

        OpenedStep? opened = null;
        await using var tx = await _invoiceDb.Database.BeginTransactionAsync();
        await CompleteStepAsync(step.Id, result, note);
        if (nextTypeStep is null)
        {
            invoice.CurrentStatus = InvoiceStatus.Completed;
            invoice.AssignedUserId = null;
        }
        else
        {
            opened = await OpenStepAsync(invoice, nextTypeStep, now);
            ApplyInvoiceState(invoice, opened.Step, nextTypeStep);
        }

        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        await tx.CommitAsync();

        await _history.LogAsync(invoice.Id, actionType, invoice.CurrentStatus, fromStatus, actorUserId, note);

        if (opened is not null)
        {
            await _history.LogAsync(
                invoice.Id,
                WorkflowActionType.Assigned,
                invoice.CurrentStatus,
                fromStatus,
                actorUserId,
                "Sonraki adıma atandı.");
            await NotifyAsync(
                invoice,
                opened,
                EmailType.InvoiceAssigned,
                NotificationType.Assigned,
                actorUserId,
                reason: null);
        }
    }

    private async Task TransitionByRuleAsync(
        int invoiceId,
        int stepId,
        int actorUserId,
        string result,
        string reason,
        WorkflowActionType trigger,
        EmailType emailType,
        NotificationType notificationType)
    {
        var (invoice, step, typeStep) = await LoadOpenStepAsync(invoiceId, stepId);
        await EnsureCanActAsync(step, actorUserId);

        var rule = await FindRuleAsync(typeStep.InvoiceTypeId, typeStep.Id, trigger);
        if (rule is null)
        {
            throw new BadRequestException(RuleMissingMessage);
        }

        var target = await _invoiceDb.InvoiceTypeSteps
            .Include(s => s.Approvers)
            .FirstOrDefaultAsync(s => s.Id == rule.TargetStepId && s.IsActive);
        if (target is null)
        {
            throw new BadRequestException("Geçiş kuralının hedef adımı aktif değil.");
        }

        await using var tx = await _invoiceDb.Database.BeginTransactionAsync();
        await CompleteStepAsync(step.Id, result, reason);

        var fromStatus = invoice.CurrentStatus;
        var now = DateTime.UtcNow;
        var opened = await OpenStepAsync(invoice, target, now);
        ApplyInvoiceState(invoice, opened.Step, target);
        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        await tx.CommitAsync();

        await _history.LogAsync(invoice.Id, trigger, invoice.CurrentStatus, fromStatus, actorUserId, reason);
        await _history.LogAsync(
            invoice.Id,
            WorkflowActionType.Assigned,
            invoice.CurrentStatus,
            fromStatus,
            actorUserId,
            "Kural ile hedef adıma atandı.");
        await NotifyAsync(invoice, opened, emailType, notificationType, actorUserId, reason);
    }

    private async Task<WorkflowTransitionRule?> FindRuleAsync(
        int invoiceTypeId,
        int currentTypeStepId,
        WorkflowActionType trigger)
    {
        return await _invoiceDb.WorkflowTransitionRules
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                r.TriggerAction == trigger &&
                r.TargetStep.InvoiceTypeId == invoiceTypeId &&
                (r.SourceStepId == null || r.SourceStepId == currentTypeStepId))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .FirstOrDefaultAsync();
    }

    private async Task CompleteStepAsync(int stepId, string result, string? reason)
    {
        var now = DateTime.UtcNow;
        var affected = await _invoiceDb.InvoiceWorkflowSteps
            .Where(s => s.Id == stepId && s.CompletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.CompletedAt, now)
                .SetProperty(s => s.Result, result)
                .SetProperty(s => s.RejectReason, reason));

        if (affected == 0)
        {
            throw new ConflictException(ConcurrentMessage);
        }
    }

    private async Task<OpenedStep> OpenStepAsync(InvoiceEntity invoice, InvoiceTypeStep typeStep, DateTime now)
    {
        int? assignedUserId = null;
        int? assignedDepartmentId = null;

        if (typeStep.DepartmentId.HasValue)
        {
            assignedDepartmentId = typeStep.DepartmentId;
        }
        else
        {
            assignedUserId = await ResolvePersonAssigneeAsync(typeStep);
        }

        DateTime? dueAt = null;
        if (typeStep.MaxDurationDays.HasValue)
        {
            dueAt = now.AddDays((double)typeStep.MaxDurationDays.Value);
        }

        var step = new InvoiceWorkflowStep
        {
            InvoiceId = invoice.Id,
            InvoiceTypeStepId = typeStep.Id,
            AssignedUserId = assignedUserId,
            AssignedDepartmentId = assignedDepartmentId,
            StartedAt = now,
            DueAt = dueAt,
            IsOverdue = false
        };

        _invoiceDb.InvoiceWorkflowSteps.Add(step);
        var recipients = await ResolveRecipientsAsync(assignedUserId, assignedDepartmentId);
        return new OpenedStep(step, recipients);
    }

    private async Task<int> ResolvePersonAssigneeAsync(InvoiceTypeStep typeStep)
    {
        var ordered = typeStep.Approvers
            .Where(a => a.IsActive)
            .OrderBy(a => a.Priority)
            .ThenBy(a => a.Id)
            .ToList();
        if (ordered.Count == 0)
        {
            throw new BadRequestException("Adımda onaylayıcı tanımlı değil.");
        }

        var userIds = ordered.Select(a => a.UserId).Distinct().ToList();
        var users = await _userDb.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.Id, u.IsOutOfOffice })
            .ToListAsync();
        var available = users.Where(u => !u.IsOutOfOffice).Select(u => u.Id).ToHashSet();
        var firstAvailable = ordered.FirstOrDefault(a => available.Contains(a.UserId));
        if (firstAvailable is not null)
        {
            return firstAvailable.UserId;
        }

        var fallback = ordered.FirstOrDefault(a => users.Any(u => u.Id == a.UserId));
        if (fallback is null)
        {
            throw new BadRequestException("Adımda geçerli bir onaylayıcı yok.");
        }

        return fallback.UserId;
    }

    private async Task<(InvoiceEntity Invoice, InvoiceWorkflowStep Step, InvoiceTypeStep TypeStep)> LoadOpenStepAsync(
        int invoiceId,
        int stepId)
    {
        var invoice = await GetRequiredInvoiceAsync(invoiceId);
        var step = await _invoiceDb.InvoiceWorkflowSteps
            .AsNoTracking()
            .Include(s => s.InvoiceTypeStep)
            .ThenInclude(t => t.Approvers)
            .FirstOrDefaultAsync(s => s.Id == stepId);
        if (step is null || step.InvoiceId != invoiceId)
        {
            throw new NotFoundException("İş akışı adımı bulunamadı.");
        }

        if (step.CompletedAt is not null)
        {
            throw new ConflictException(ConcurrentMessage);
        }

        var openId = await _invoiceDb.InvoiceWorkflowSteps
            .AsNoTracking()
            .Where(s => s.InvoiceId == invoiceId && s.CompletedAt == null)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (openId != step.Id)
        {
            throw new BadRequestException("Bu adım faturanın güncel adımı değil.");
        }

        return (invoice, step, step.InvoiceTypeStep);
    }

    private async Task EnsureCanActAsync(InvoiceWorkflowStep step, int actorUserId)
    {
        if (step.AssignedUserId.HasValue)
        {
            if (step.AssignedUserId.Value != actorUserId)
            {
                throw new ForbiddenException(ActForbiddenMessage);
            }

            return;
        }

        if (step.AssignedDepartmentId.HasValue)
        {
            var inDepartment = await ActorInDepartmentAsync(actorUserId, step.AssignedDepartmentId.Value);
            if (!inDepartment)
            {
                throw new ForbiddenException(ActForbiddenMessage);
            }

            return;
        }

        throw new ForbiddenException(ActForbiddenMessage);
    }

    private async Task<bool> ActorInDepartmentAsync(int userId, int departmentId)
    {
        var now = DateTime.UtcNow;
        return await _userDb.UserRoles
            .AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive &&
                ur.Role.DepartmentId == departmentId);
    }

    private async Task<List<MailRecipient>> ResolveRecipientsAsync(int? assignedUserId, int? assignedDepartmentId)
    {
        if (assignedUserId.HasValue)
        {
            var user = await _userDb.Users
                .AsNoTracking()
                .Where(u => u.Id == assignedUserId.Value)
                .Select(u => new MailRecipient(u.Id, u.Email, u.FullName))
                .FirstOrDefaultAsync();
            return user is null || string.IsNullOrWhiteSpace(user.Email) ? [] : [user];
        }

        if (!assignedDepartmentId.HasValue)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var rows = await _userDb.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive &&
                ur.Role.DepartmentId == assignedDepartmentId.Value &&
                ur.Role.RolePermissions.Any(rp =>
                    rp.Permission.IsActive &&
                    rp.Permission.Name == ApproverPermission) &&
                ur.User.IsActive)
            .Select(ur => new { ur.UserId, ur.User.Email, ur.User.FullName })
            .ToListAsync();

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .GroupBy(r => r.UserId)
            .Select(g =>
            {
                var row = g.First();
                return new MailRecipient(row.UserId, row.Email, row.FullName);
            })
            .ToList();
    }

    private async Task NotifyAsync(
        InvoiceEntity invoice,
        OpenedStep opened,
        EmailType emailType,
        NotificationType notificationType,
        int? actorUserId,
        string? reason)
    {
        var recipients = opened.Recipients
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        string? actorEmail = null;
        if (actorUserId.HasValue)
        {
            actorEmail = await _userDb.Users
                .AsNoTracking()
                .Where(u => u.Id == actorUserId.Value)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
        }

        var cc = string.IsNullOrWhiteSpace(actorEmail) ? null : new[] { actorEmail };

        foreach (var recipient in recipients)
        {
            var placeholders = new Dictionary<string, string>
            {
                ["FullName"] = recipient.FullName,
                ["InvoiceNumber"] = invoice.InvoiceNumber,
                ["Reason"] = reason ?? string.Empty
            };

            try
            {
                await _email.SendAsync(emailType, [recipient.Email], cc, placeholders);
                await _notifications.LogAsync(
                    invoice.Id,
                    recipient.Email,
                    notificationType,
                    recipient.UserId,
                    opened.Step.Id,
                    isSuccess: true);
            }
            catch (Exception ex)
            {
                await _notifications.LogAsync(
                    invoice.Id,
                    recipient.Email,
                    notificationType,
                    recipient.UserId,
                    opened.Step.Id,
                    isSuccess: false,
                    errorMessage: ex.Message);
            }
        }
    }

    private async Task<InvoiceEntity> GetRequiredInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceDb.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        return invoice;
    }

    private IQueryable<InvoiceTypeStep> QueryActiveTypeSteps(int invoiceTypeId)
    {
        return _invoiceDb.InvoiceTypeSteps
            .Include(s => s.Approvers)
            .Where(s => s.InvoiceTypeId == invoiceTypeId && s.IsActive)
            .OrderBy(s => s.StepOrder);
    }

    private static void ApplyInvoiceState(InvoiceEntity invoice, InvoiceWorkflowStep step, InvoiceTypeStep typeStep)
    {
        invoice.AssignedUserId = step.AssignedUserId;
        invoice.CurrentStatus = StatusFromTag(typeStep.StepRoleTag);
    }

    private static InvoiceStatus StatusFromTag(string? tag)
    {
        var normalized = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ERP_CONTROL" => InvoiceStatus.PendingErpCheck,
            "ACCOUNTING" => InvoiceStatus.PendingAccounting,
            "ARCHIVE" => InvoiceStatus.Archived,
            "AUDITOR2" => InvoiceStatus.PendingAuditor2,
            "AUDITOR" or "AUDITOR1" => InvoiceStatus.PendingAuditor1,
            _ => InvoiceStatus.InDepartmentChain
        };
    }

    private static WorkflowActionType RejectTriggerFromTag(string? tag)
    {
        var normalized = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim().ToUpperInvariant();
        return normalized switch
        {
            "AUDITOR2" => WorkflowActionType.Auditor2Rejected,
            "AUDITOR" or "AUDITOR1" => WorkflowActionType.Auditor1Rejected,
            _ => WorkflowActionType.Rejected
        };
    }

    private static string RequiredReason(string reason)
    {
        var trimmed = reason.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("Gerekçe zorunludur.");
        }

        return trimmed;
    }

    private static string? TrimToNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed record OpenedStep(InvoiceWorkflowStep Step, List<MailRecipient> Recipients);

    private sealed record MailRecipient(int UserId, string Email, string FullName);
}
