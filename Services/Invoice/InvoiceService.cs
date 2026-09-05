using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = InvoiceTrackingSystemBackend.Entities.Invoice.Invoice;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceService : IInvoiceService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;
    private readonly IInvoiceActivityLogService _activityLog;
    private readonly IInvoiceWorkflowHistoryService _workflowHistory;
    private readonly IInvoiceAccessService _access;

    public InvoiceService(
        InvoiceDbContext invoiceDb,
        AuthReferenceLookup authLookup,
        IInvoiceActivityLogService activityLog,
        IInvoiceWorkflowHistoryService workflowHistory,
        IInvoiceAccessService access)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
        _activityLog = activityLog;
        _workflowHistory = workflowHistory;
        _access = access;
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        InvoiceStatus? status = null,
        int? supplierId = null,
        int? invoiceTypeId = null,
        bool? isDuplicate = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var access = await _access.ResolveAsync();
        if (!access.CanAccessAll && access.InvoiceTypeIds.Count == 0)
        {
            return PagedResult<InvoiceListItemDto>.Create([], 0, page, pageSize);
        }

        var query = _invoiceDb.Invoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.InvoiceType)
            .AsQueryable();

        if (!access.CanAccessAll)
        {
            var typeIds = access.InvoiceTypeIds.ToList();
            query = query.Where(i => i.InvoiceTypeId != null && typeIds.Contains(i.InvoiceTypeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term) ||
                i.VknTckn.Contains(term) ||
                (i.Supplier != null && i.Supplier.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.CurrentStatus == status.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (invoiceTypeId.HasValue)
        {
            query = query.Where(i => i.InvoiceTypeId == invoiceTypeId.Value);
        }

        if (isDuplicate.HasValue)
        {
            query = query.Where(i => i.IsDuplicate == isDuplicate.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var currentSteps = await LoadCurrentStepsAsync(invoices.Select(i => i.Id));
        var items = invoices.Select(i =>
        {
            currentSteps.TryGetValue(i.Id, out var step);
            return MapList(i, step);
        }).ToList();

        return PagedResult<InvoiceListItemDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<InvoiceDetailDto> GetByIdAsync(int id, int? viewerUserId = null)
    {
        var invoice = await LoadDetailAsync(id);
        var access = await _access.ResolveAsync();
        access.EnsureCanRead(invoice.InvoiceTypeId);
        var dto = await MapDetailAsync(invoice);

        if (viewerUserId.HasValue)
        {
            await _activityLog.LogAsync(id, InvoiceActivityType.VIEWED, viewerUserId);
        }

        return dto;
    }

    public async Task<InvoiceDetailDto> CreateAsync(CreateInvoiceRequestDto request, int? actorUserId = null)
    {
        var vkn = NormalizeVkn(request.VknTckn);
        var invoiceNumber = NormalizeInvoiceNumber(request.InvoiceNumber);
        await EnsureNotDuplicateAsync(vkn, invoiceNumber, request.Amount);

        var supplier = await ResolveSupplierAsync(vkn, request.SupplierId);
        var invoiceTypeId = await ResolveInvoiceTypeIdAsync(request.InvoiceTypeId, supplier?.Id);
        var access = await _access.ResolveAsync();
        access.EnsureCanCreate(invoiceTypeId);
        var assignmentMethod = request.InvoiceTypeId.HasValue
            ? AssignmentMethod.Manual
            : invoiceTypeId.HasValue
                ? AssignmentMethod.SupplierSingleCandidate
                : (AssignmentMethod?)null;

        var now = DateTime.UtcNow;
        var invoice = new InvoiceEntity
        {
            ExternalInvoiceId = NormalizeOptional(request.ExternalInvoiceId, 100),
            VknTckn = vkn,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate,
            Amount = request.Amount,
            Currency = request.Currency,
            SupplierId = supplier?.Id,
            CurrentAccountCode = NormalizeOptional(request.CurrentAccountCode, 50),
            RawPayloadJson = string.IsNullOrWhiteSpace(request.RawPayloadJson) ? null : request.RawPayloadJson.Trim(),
            AmountTry = request.Currency == Currency.Try ? request.Amount : null,
            InvoiceTypeId = invoiceTypeId,
            AssignmentMethod = assignmentMethod,
            CurrentStatus = InvoiceStatus.Received,
            CreatedAt = now
        };

        foreach (var line in request.LineItems)
        {
            invoice.LineItems.Add(MapNewLineItem(line, now));
        }

        _invoiceDb.Invoices.Add(invoice);
        await _invoiceDb.SaveChangesAsync();

        await _workflowHistory.LogAsync(
            invoice.Id,
            WorkflowActionType.StatusChanged,
            InvoiceStatus.Received,
            fromStatus: null,
            actorUserId,
            reason: "Fatura kaydı oluşturuldu.");

        return await MapDetailAsync(await LoadDetailAsync(invoice.Id));
    }

    public async Task<InvoiceDetailDto> UpdateAsync(int id, UpdateInvoiceRequestDto request)
    {
        var invoice = await GetRequiredAsync(id);
        var access = await _access.ResolveAsync();
        access.EnsureCanWrite(invoice.InvoiceTypeId);
        var now = DateTime.UtcNow;

        if (request.SupplierId.HasValue)
        {
            var supplier = await _invoiceDb.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId.Value);
            if (supplier is null || !supplier.IsActive)
            {
                throw new BadRequestException("Geçerli bir tedarikçi seçilmedi.");
            }

            if (supplier.VknTckn != invoice.VknTckn)
            {
                throw new BadRequestException("Tedarikçi VKN/TCKN'si fatura ile eşleşmiyor.");
            }

            invoice.SupplierId = supplier.Id;
        }

        if (request.InvoiceTypeId.HasValue && request.InvoiceTypeId != invoice.InvoiceTypeId)
        {
            var hasSteps = await _invoiceDb.InvoiceWorkflowSteps.AnyAsync(s => s.InvoiceId == id);
            if (hasSteps)
            {
                throw new BadRequestException("İş akışı başlamış faturanın türü değiştirilemez.");
            }

            await EnsureInvoiceTypeExistsAsync(request.InvoiceTypeId);
            access.EnsureCanWrite(request.InvoiceTypeId);
            invoice.InvoiceTypeId = request.InvoiceTypeId;
            invoice.AssignmentMethod = request.AssignmentMethod ?? AssignmentMethod.Manual;
        }
        else if (request.AssignmentMethod.HasValue)
        {
            invoice.AssignmentMethod = request.AssignmentMethod;
        }

        await _authLookup.EnsureUsersExistAsync(
            new[] { request.AssignedUserId, request.Auditor1UserId, request.Auditor2UserId }
                .Where(x => x.HasValue)
                .Select(x => x!.Value));

        invoice.AssignedUserId = request.AssignedUserId;
        invoice.Auditor1UserId = request.Auditor1UserId;
        invoice.Auditor2UserId = request.Auditor2UserId;
        invoice.CurrentAccountCode = NormalizeOptional(request.CurrentAccountCode, 50);
        invoice.ErrorReason = NormalizeOptional(request.ErrorReason, 500);
        invoice.UpdatedAt = now;

        await _invoiceDb.SaveChangesAsync();
        return await MapDetailAsync(await LoadDetailAsync(id));
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await _invoiceDb.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        var access = await _access.ResolveAsync();
        access.EnsureCanDelete(invoice.InvoiceTypeId);

        var now = DateTime.UtcNow;
        invoice.DeletedAt = now;
        invoice.UpdatedAt = now;
        invoice.InvoiceNumber = UniqueDeletedInvoiceNumber(invoice.Id, invoice.InvoiceNumber);

        foreach (var line in invoice.LineItems.Where(l => l.DeletedAt is null))
        {
            line.DeletedAt = now;
            line.UpdatedAt = now;
        }

        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<InvoiceLineItemResponseDto>> GetLineItemsAsync(int invoiceId)
    {
        await EnsureCanReadInvoiceAsync(invoiceId);

        var items = await _invoiceDb.InvoiceLineItems
            .AsNoTracking()
            .Where(l => l.InvoiceId == invoiceId)
            .OrderBy(l => l.Id)
            .ToListAsync();

        return items.Select(MapLineItem).ToList();
    }

    public async Task<InvoiceLineItemResponseDto> CreateLineItemAsync(int invoiceId, CreateInvoiceLineItemRequestDto request)
    {
        await EnsureCanWriteInvoiceAsync(invoiceId);

        var line = MapNewLineItem(request, DateTime.UtcNow);
        line.InvoiceId = invoiceId;
        _invoiceDb.InvoiceLineItems.Add(line);
        await _invoiceDb.SaveChangesAsync();

        return MapLineItem(line);
    }

    public async Task<InvoiceLineItemResponseDto> UpdateLineItemAsync(
        int invoiceId,
        int lineItemId,
        UpdateInvoiceLineItemRequestDto request)
    {
        await EnsureCanWriteInvoiceAsync(invoiceId);
        var line = await GetRequiredLineItemAsync(invoiceId, lineItemId);
        ApplyLineItem(line, request.Description, request.Quantity, request.UnitPrice, request.LineAmount);
        line.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();
        return MapLineItem(line);
    }

    public async Task DeleteLineItemAsync(int invoiceId, int lineItemId)
    {
        await EnsureCanWriteInvoiceAsync(invoiceId);
        var line = await GetRequiredLineItemAsync(invoiceId, lineItemId);
        var now = DateTime.UtcNow;
        line.DeletedAt = now;
        line.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<InvoiceRelationResponseDto>> GetRelationsAsync(int invoiceId)
    {
        await EnsureCanReadInvoiceAsync(invoiceId);
        var relations = await LoadRelationsAsync(invoiceId);
        return await MapRelationsAsync(relations);
    }

    public async Task<InvoiceRelationResponseDto> CreateRelationAsync(
        int invoiceId,
        CreateInvoiceRelationRequestDto request,
        int? createdByUserId)
    {
        if (invoiceId == request.RelatedInvoiceId)
        {
            throw new BadRequestException("Fatura kendisiyle ilişkilendirilemez.");
        }

        await EnsureCanWriteInvoiceAsync(invoiceId);
        await EnsureCanReadInvoiceAsync(request.RelatedInvoiceId);

        var exists = await _invoiceDb.InvoiceRelations.AnyAsync(r =>
            r.InvoiceId == invoiceId &&
            r.RelatedInvoiceId == request.RelatedInvoiceId &&
            r.RelationType == request.RelationType);
        if (exists)
        {
            throw new ConflictException("Bu fatura ilişkisi zaten kayıtlı.");
        }

        var relation = new InvoiceRelation
        {
            InvoiceId = invoiceId,
            RelatedInvoiceId = request.RelatedInvoiceId,
            RelationType = request.RelationType,
            Note = NormalizeOptional(request.Note, 500),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        _invoiceDb.InvoiceRelations.Add(relation);

        if (request.RelationType == InvoiceRelationType.DuplicateOf)
        {
            var invoice = await GetRequiredAsync(invoiceId);
            invoice.IsDuplicate = true;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        await _invoiceDb.SaveChangesAsync();

        var loaded = await _invoiceDb.InvoiceRelations
            .AsNoTracking()
            .Include(r => r.Invoice)
            .Include(r => r.RelatedInvoice)
            .FirstAsync(r => r.Id == relation.Id);

        var mapped = await MapRelationsAsync([loaded]);
        return mapped[0];
    }

    public async Task<IReadOnlyList<InvoiceWorkflowStepResponseDto>> GetWorkflowStepsAsync(int invoiceId)
    {
        var invoice = await LoadDetailAsync(invoiceId);
        (await _access.ResolveAsync()).EnsureCanRead(invoice.InvoiceTypeId);
        return await MapWorkflowStepsAsync(invoice.WorkflowSteps.ToList());
    }

    public async Task<IReadOnlyList<InvoiceAttachmentResponseDto>> GetAttachmentsAsync(int invoiceId)
    {
        var invoice = await LoadDetailAsync(invoiceId);
        (await _access.ResolveAsync()).EnsureCanRead(invoice.InvoiceTypeId);
        return await MapAttachmentsAsync(invoice.Attachments.ToList());
    }

    private async Task<InvoiceEntity> LoadDetailAsync(int id)
    {
        var invoice = await _invoiceDb.Invoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.InvoiceType)
            .Include(i => i.ExchangeRate)
            .Include(i => i.LineItems)
            .Include(i => i.WorkflowSteps)
                .ThenInclude(s => s.InvoiceTypeStep)
            .Include(i => i.Attachments)
            .Include(i => i.Relations)
                .ThenInclude(r => r.RelatedInvoice)
            .Include(i => i.RelatedToRelations)
                .ThenInclude(r => r.Invoice)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        return invoice;
    }

    private async Task<InvoiceEntity> GetRequiredAsync(int id)
    {
        var invoice = await _invoiceDb.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        return invoice;
    }

    private async Task EnsureCanReadInvoiceAsync(int invoiceId)
    {
        var typeId = await GetInvoiceTypeIdAsync(invoiceId);
        (await _access.ResolveAsync()).EnsureCanRead(typeId);
    }

    private async Task EnsureCanWriteInvoiceAsync(int invoiceId)
    {
        var typeId = await GetInvoiceTypeIdAsync(invoiceId);
        (await _access.ResolveAsync()).EnsureCanWrite(typeId);
    }

    private async Task<int?> GetInvoiceTypeIdAsync(int invoiceId)
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

        return invoice.InvoiceTypeId;
    }

    private async Task<InvoiceLineItem> GetRequiredLineItemAsync(int invoiceId, int lineItemId)
    {
        var line = await _invoiceDb.InvoiceLineItems
            .FirstOrDefaultAsync(l => l.Id == lineItemId && l.InvoiceId == invoiceId);
        if (line is null)
        {
            throw new NotFoundException("Fatura kalemi bulunamadı.");
        }

        return line;
    }

    private async Task EnsureNotDuplicateAsync(string vkn, string invoiceNumber, decimal amount)
    {
        var exists = await _invoiceDb.Invoices.AnyAsync(i =>
            i.VknTckn == vkn &&
            i.InvoiceNumber == invoiceNumber &&
            i.Amount == amount);
        if (exists)
        {
            throw new ConflictException("Aynı VKN, fatura numarası ve tutarla kayıt zaten var.");
        }
    }

    private async Task<Supplier?> ResolveSupplierAsync(string vkn, int? supplierId)
    {
        if (supplierId.HasValue)
        {
            var supplier = await _invoiceDb.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId.Value);
            if (supplier is null || !supplier.IsActive)
            {
                throw new BadRequestException("Geçerli bir tedarikçi seçilmedi.");
            }

            if (supplier.VknTckn != vkn)
            {
                throw new BadRequestException("Tedarikçi VKN/TCKN'si fatura ile eşleşmiyor.");
            }

            return supplier;
        }

        return await _invoiceDb.Suppliers.FirstOrDefaultAsync(s => s.VknTckn == vkn && s.IsActive);
    }

    private async Task<int?> ResolveInvoiceTypeIdAsync(int? requestedTypeId, int? supplierId)
    {
        if (requestedTypeId.HasValue)
        {
            await EnsureInvoiceTypeExistsAsync(requestedTypeId);
            return requestedTypeId;
        }

        if (!supplierId.HasValue)
        {
            return null;
        }

        var candidateIds = await _invoiceDb.SupplierInvoiceTypes
            .Where(sit => sit.SupplierId == supplierId.Value && sit.IsActive && sit.InvoiceType.IsActive)
            .Select(sit => sit.InvoiceTypeId)
            .Distinct()
            .ToListAsync();

        return candidateIds.Count == 1 ? candidateIds[0] : null;
    }

    private async Task EnsureInvoiceTypeExistsAsync(int? invoiceTypeId)
    {
        if (!invoiceTypeId.HasValue)
        {
            return;
        }

        var exists = await _invoiceDb.InvoiceTypes
            .AsNoTracking()
            .AnyAsync(t => t.Id == invoiceTypeId.Value && t.IsActive);
        if (!exists)
        {
            throw new BadRequestException("Geçerli bir fatura türü seçilmedi.");
        }
    }

    private async Task<Dictionary<int, InvoiceWorkflowStep>> LoadCurrentStepsAsync(IEnumerable<int> invoiceIds)
    {
        var ids = invoiceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var steps = await _invoiceDb.InvoiceWorkflowSteps
            .AsNoTracking()
            .Include(s => s.InvoiceTypeStep)
            .Where(s => ids.Contains(s.InvoiceId) && s.CompletedAt == null)
            .ToListAsync();

        return steps
            .GroupBy(s => s.InvoiceId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.StartedAt).ThenByDescending(s => s.Id).First());
    }

    private async Task<List<InvoiceRelation>> LoadRelationsAsync(int invoiceId)
    {
        return await _invoiceDb.InvoiceRelations
            .AsNoTracking()
            .Include(r => r.Invoice)
            .Include(r => r.RelatedInvoice)
            .Where(r => r.InvoiceId == invoiceId || r.RelatedInvoiceId == invoiceId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    private async Task<InvoiceDetailDto> MapDetailAsync(InvoiceEntity invoice)
    {
        var userIds = new List<int>();
        AddIfHasValue(userIds, invoice.AssignedUserId);
        AddIfHasValue(userIds, invoice.Auditor1UserId);
        AddIfHasValue(userIds, invoice.Auditor2UserId);
        userIds.AddRange(invoice.WorkflowSteps.Where(s => s.AssignedUserId.HasValue).Select(s => s.AssignedUserId!.Value));
        userIds.AddRange(invoice.Attachments.Where(a => a.UploadedByUserId.HasValue).Select(a => a.UploadedByUserId!.Value));

        var relations = invoice.Relations
            .Concat(invoice.RelatedToRelations)
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .ToList();
        userIds.AddRange(relations.Where(r => r.CreatedByUserId.HasValue).Select(r => r.CreatedByUserId!.Value));

        var userNames = await _authLookup.GetUserFullNamesAsync(userIds);
        var departmentNames = await _authLookup.GetDepartmentNamesAsync(
            invoice.WorkflowSteps.Where(s => s.AssignedDepartmentId.HasValue).Select(s => s.AssignedDepartmentId!.Value));

        return new InvoiceDetailDto
        {
            Id = invoice.Id,
            ExternalInvoiceId = invoice.ExternalInvoiceId,
            VknTckn = invoice.VknTckn,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            SupplierId = invoice.SupplierId,
            SupplierName = invoice.Supplier?.Name,
            CurrentAccountCode = invoice.CurrentAccountCode,
            ExchangeRateId = invoice.ExchangeRateId,
            ExchangeRateToTry = invoice.ExchangeRate?.RateToTry,
            AmountTry = invoice.AmountTry,
            ErpMatchStatus = invoice.ErpMatchStatus,
            ErpCheckedAt = invoice.ErpCheckedAt,
            ErpSource = invoice.ErpSource,
            IsDuplicate = invoice.IsDuplicate,
            ErrorReason = invoice.ErrorReason,
            InvoiceTypeId = invoice.InvoiceTypeId,
            InvoiceTypeName = invoice.InvoiceType?.Name,
            PredictionConfidence = invoice.PredictionConfidence,
            PredictionModelVersion = invoice.PredictionModelVersion,
            AssignmentMethod = invoice.AssignmentMethod,
            AssignedUserId = invoice.AssignedUserId,
            AssignedUserFullName = NameOf(userNames, invoice.AssignedUserId),
            Auditor1UserId = invoice.Auditor1UserId,
            Auditor1UserFullName = NameOf(userNames, invoice.Auditor1UserId),
            Auditor2UserId = invoice.Auditor2UserId,
            Auditor2UserFullName = NameOf(userNames, invoice.Auditor2UserId),
            CurrentStatus = invoice.CurrentStatus,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            LineItems = invoice.LineItems.OrderBy(l => l.Id).Select(MapLineItem).ToList(),
            WorkflowSteps = MapWorkflowSteps(invoice.WorkflowSteps, userNames, departmentNames),
            Attachments = MapAttachments(invoice.Attachments, userNames),
            Relations = MapRelations(relations, userNames)
        };
    }

    private async Task<List<InvoiceWorkflowStepResponseDto>> MapWorkflowStepsAsync(
        IReadOnlyList<InvoiceWorkflowStep> steps)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            steps.Where(s => s.AssignedUserId.HasValue).Select(s => s.AssignedUserId!.Value));
        var departmentNames = await _authLookup.GetDepartmentNamesAsync(
            steps.Where(s => s.AssignedDepartmentId.HasValue).Select(s => s.AssignedDepartmentId!.Value));
        return MapWorkflowSteps(steps, userNames, departmentNames);
    }

    private async Task<List<InvoiceAttachmentResponseDto>> MapAttachmentsAsync(
        IReadOnlyList<InvoiceAttachment> attachments)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            attachments.Where(a => a.UploadedByUserId.HasValue).Select(a => a.UploadedByUserId!.Value));
        return MapAttachments(attachments, userNames);
    }

    private async Task<List<InvoiceRelationResponseDto>> MapRelationsAsync(IReadOnlyList<InvoiceRelation> relations)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            relations.Where(r => r.CreatedByUserId.HasValue).Select(r => r.CreatedByUserId!.Value));
        return MapRelations(relations, userNames);
    }

    private static InvoiceListItemDto MapList(InvoiceEntity invoice, InvoiceWorkflowStep? currentStep)
    {
        return new InvoiceListItemDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            VknTckn = invoice.VknTckn,
            SupplierName = invoice.Supplier?.Name,
            InvoiceTypeName = invoice.InvoiceType?.Name,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            AmountTry = invoice.AmountTry,
            CurrentStatus = invoice.CurrentStatus,
            IsDuplicate = invoice.IsDuplicate,
            CurrentStepName = currentStep?.InvoiceTypeStep?.StepName,
            CurrentStepAssignedUserId = currentStep?.AssignedUserId,
            CurrentStepDueAt = currentStep?.DueAt,
            CurrentStepIsOverdue = currentStep?.IsOverdue
        };
    }

    private static InvoiceLineItem MapNewLineItem(CreateInvoiceLineItemRequestDto request, DateTime now)
    {
        var line = new InvoiceLineItem
        {
            CreatedAt = now
        };
        ApplyLineItem(line, request.Description, request.Quantity, request.UnitPrice, request.LineAmount);
        return line;
    }

    private static void ApplyLineItem(
        InvoiceLineItem line,
        string description,
        decimal? quantity,
        decimal? unitPrice,
        decimal? lineAmount)
    {
        line.Description = description.Trim();
        line.Quantity = quantity;
        line.UnitPrice = unitPrice;
        line.LineAmount = lineAmount ?? (quantity.HasValue && unitPrice.HasValue
            ? decimal.Round(quantity.Value * unitPrice.Value, 2, MidpointRounding.AwayFromZero)
            : null);
    }

    private static InvoiceLineItemResponseDto MapLineItem(InvoiceLineItem line)
    {
        return new InvoiceLineItemResponseDto
        {
            Id = line.Id,
            InvoiceId = line.InvoiceId,
            Description = line.Description,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineAmount = line.LineAmount
        };
    }

    private static List<InvoiceWorkflowStepResponseDto> MapWorkflowSteps(
        IEnumerable<InvoiceWorkflowStep> steps,
        IReadOnlyDictionary<int, string> userNames,
        IReadOnlyDictionary<int, string> departmentNames)
    {
        return steps
            .OrderBy(s => s.StartedAt)
            .ThenBy(s => s.Id)
            .Select(s => new InvoiceWorkflowStepResponseDto
            {
                Id = s.Id,
                InvoiceId = s.InvoiceId,
                InvoiceTypeStepId = s.InvoiceTypeStepId,
                StepOrder = s.InvoiceTypeStep?.StepOrder ?? 0,
                StepName = s.InvoiceTypeStep?.StepName ?? string.Empty,
                AssignedUserId = s.AssignedUserId,
                AssignedUserFullName = NameOf(userNames, s.AssignedUserId),
                AssignedDepartmentId = s.AssignedDepartmentId,
                AssignedDepartmentName = s.AssignedDepartmentId.HasValue &&
                    departmentNames.TryGetValue(s.AssignedDepartmentId.Value, out var deptName)
                    ? deptName
                    : null,
                StartedAt = s.StartedAt,
                DueAt = s.DueAt,
                CompletedAt = s.CompletedAt,
                ReminderSentAt = s.ReminderSentAt,
                IsOverdue = s.IsOverdue,
                Result = s.Result,
                RejectReason = s.RejectReason
            })
            .ToList();
    }

    private static List<InvoiceAttachmentResponseDto> MapAttachments(
        IEnumerable<InvoiceAttachment> attachments,
        IReadOnlyDictionary<int, string> userNames)
    {
        return attachments
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new InvoiceAttachmentResponseDto
            {
                Id = a.Id,
                InvoiceId = a.InvoiceId,
                FileName = a.FileName,
                NasRelativePath = a.NasRelativePath,
                FileType = a.FileType,
                FileSizeBytes = a.FileSizeBytes,
                ChecksumSha256 = a.ChecksumSha256,
                UploadedByUserId = a.UploadedByUserId,
                UploadedByUserFullName = NameOf(userNames, a.UploadedByUserId),
                UploadedAt = a.UploadedAt
            })
            .ToList();
    }

    private static List<InvoiceRelationResponseDto> MapRelations(
        IEnumerable<InvoiceRelation> relations,
        IReadOnlyDictionary<int, string> userNames)
    {
        return relations
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new InvoiceRelationResponseDto
            {
                Id = r.Id,
                InvoiceId = r.InvoiceId,
                InvoiceNumber = r.Invoice?.InvoiceNumber ?? string.Empty,
                RelatedInvoiceId = r.RelatedInvoiceId,
                RelatedInvoiceNumber = r.RelatedInvoice?.InvoiceNumber ?? string.Empty,
                RelationType = r.RelationType,
                Note = r.Note,
                CreatedByUserId = r.CreatedByUserId,
                CreatedByUserFullName = NameOf(userNames, r.CreatedByUserId),
                CreatedAt = r.CreatedAt
            })
            .ToList();
    }

    private static void AddIfHasValue(List<int> ids, int? value)
    {
        if (value.HasValue)
        {
            ids.Add(value.Value);
        }
    }

    private static string? NameOf(IReadOnlyDictionary<int, string> names, int? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        return names.TryGetValue(userId.Value, out var name) ? name : null;
    }

    private static string NormalizeVkn(string vknTckn)
    {
        var trimmed = vknTckn.Trim();
        if (trimmed.Length is not (10 or 11) || !trimmed.All(char.IsDigit))
        {
            throw new BadRequestException("VKN 10, TCKN 11 haneli ve yalnızca rakam olmalıdır.");
        }

        return trimmed;
    }

    private static string NormalizeInvoiceNumber(string invoiceNumber)
    {
        var trimmed = invoiceNumber.Trim();
        if (trimmed.Length == 0)
        {
            throw new BadRequestException("Fatura numarası boş olamaz.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string UniqueDeletedInvoiceNumber(int id, string original)
    {
        var prefix = $"D{id}_";
        var remaining = 50 - prefix.Length;
        if (remaining <= 0)
        {
            return prefix[..50];
        }

        var suffix = original.Length <= remaining ? original : original[^remaining..];
        return prefix + suffix;
    }
}
