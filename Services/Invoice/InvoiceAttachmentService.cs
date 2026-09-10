using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using InvoiceTrackingSystemBackend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using InvoiceEntity = InvoiceTrackingSystemBackend.Entities.Invoice.Invoice;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceAttachmentService : IInvoiceAttachmentService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;
    private readonly IInvoiceActivityLogService _activityLog;
    private readonly IInvoiceAccessService _access;
    private readonly IInvoiceWorkflowService _workflow;
    private readonly IStorageService _storage;
    private readonly StorageOptions _storageOptions;

    private static readonly Dictionary<string, (string Extension, string FileType)> AllowedAttachmentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = (".pdf", "PDF"),
            ["image/png"] = (".png", "IMAGE"),
            ["image/jpeg"] = (".jpg", "IMAGE"),
            ["image/webp"] = (".webp", "IMAGE")
        };

    public InvoiceAttachmentService(
        InvoiceDbContext invoiceDb,
        AuthReferenceLookup authLookup,
        IInvoiceActivityLogService activityLog,
        IInvoiceAccessService access,
        IInvoiceWorkflowService workflow,
        IStorageService storage,
        IOptions<StorageOptions> storageOptions)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
        _activityLog = activityLog;
        _access = access;
        _workflow = workflow;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    public async Task<PagedResult<InvoiceAttachmentResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? workflowStepId = null,
        int? uploadedByUserId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var access = await _access.ResolveAsync();
        var query = await BuildVisibleQueryAsync(access, invoiceId);

        if (workflowStepId.HasValue)
        {
            query = query.Where(a => a.WorkflowStepId == workflowStepId.Value);
        }

        if (uploadedByUserId.HasValue)
        {
            query = query.Where(a => a.UploadedByUserId == uploadedByUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.FileName.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var entities = await query
            .OrderByDescending(a => a.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = await MapManyAsync(entities);
        return PagedResult<InvoiceAttachmentResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(int? invoiceId = null)
    {
        var access = await _access.ResolveAsync();
        var query = await BuildVisibleQueryAsync(access, invoiceId);

        var rows = await query
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new { a.Id, a.FileName, a.InvoiceId })
            .ToListAsync();

        return rows.Select(a => new IdNameDto
        {
            Id = a.Id,
            Name = $"{a.FileName} #{a.InvoiceId}"
        }).ToList();
    }

    public async Task<InvoiceAttachmentResponseDto> GetByIdAsync(int id)
    {
        var attachment = await GetRequiredAsync(id, tracking: false);
        await EnsureCanReadInvoiceAsync(attachment.InvoiceId);
        var items = await MapManyAsync([attachment]);
        return items[0];
    }

    public async Task<IReadOnlyList<InvoiceAttachmentResponseDto>> CreateAsync(
        int invoiceId,
        int actorUserId,
        IReadOnlyList<IFormFile> files)
    {
        if (invoiceId < 1)
        {
            throw new BadRequestException("Geçerli bir fatura seçilmedi.");
        }

        var invoice = await GetRequiredInvoiceAsync(invoiceId);
        var stepId = await _workflow.RequireOpenStepForActorAsync(invoiceId, actorUserId);
        var validFiles = (files ?? []).Where(f => f is { Length: > 0 }).ToList();
        if (validFiles.Count == 0)
        {
            throw new BadRequestException("Dosya seçilmedi.");
        }

        var now = DateTime.UtcNow;
        var created = new List<InvoiceAttachment>();
        foreach (var file in validFiles)
        {
            created.Add(await SaveNewFileAsync(invoiceId, stepId, actorUserId, file, now));
        }

        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();

        foreach (var attachment in created)
        {
            await _activityLog.LogAsync(
                invoiceId,
                InvoiceActivityType.ATTACHMENT_UPLOADED,
                actorUserId,
                stepId,
                attachment.FileName);
        }

        return await MapManyAsync(created);
    }

    public async Task<InvoiceAttachmentResponseDto> UpdateAsync(
        int id,
        int actorUserId,
        string? fileName,
        IFormFile? file)
    {
        var trimmedName = string.IsNullOrWhiteSpace(fileName) ? null : Path.GetFileName(fileName.Trim());
        var hasFile = file is { Length: > 0 };
        if (trimmedName is null && !hasFile)
        {
            throw new BadRequestException("Güncellenecek alan yok.");
        }

        var attachment = await GetRequiredAsync(id, tracking: true);
        await EnsureCanMutateAsync(attachment, actorUserId);

        var now = DateTime.UtcNow;
        if (hasFile)
        {
            var saved = await StoreFileAsync(attachment.InvoiceId, attachment.WorkflowStepId, file!);
            attachment.NasRelativePath = saved.RelativePath;
            attachment.FileType = saved.FileType;
            attachment.FileSizeBytes = (int)file!.Length;
            attachment.ChecksumSha256 = saved.Checksum;
            if (trimmedName is null)
            {
                trimmedName = saved.OriginalName;
            }
        }

        if (trimmedName is not null)
        {
            if (trimmedName.Length > 255)
            {
                trimmedName = trimmedName[..255];
            }

            attachment.FileName = trimmedName;
        }

        var invoice = await GetRequiredInvoiceAsync(attachment.InvoiceId);
        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        await _activityLog.LogAsync(
            attachment.InvoiceId,
            InvoiceActivityType.ATTACHMENT_UPDATED,
            actorUserId,
            attachment.WorkflowStepId,
            attachment.FileName);

        var items = await MapManyAsync([attachment]);
        return items[0];
    }

    public async Task DeleteAsync(int id, int actorUserId)
    {
        var attachment = await GetRequiredAsync(id, tracking: true);
        await EnsureCanMutateAsync(attachment, actorUserId);

        var now = DateTime.UtcNow;
        attachment.DeletedAt = now;
        var invoice = await GetRequiredInvoiceAsync(attachment.InvoiceId);
        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        await _activityLog.LogAsync(
            attachment.InvoiceId,
            InvoiceActivityType.ATTACHMENT_DELETED,
            actorUserId,
            attachment.WorkflowStepId,
            attachment.FileName);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenContentAsync(
        int id,
        int actorUserId)
    {
        var attachment = await GetRequiredAsync(id, tracking: false);
        await EnsureCanReadInvoiceAsync(attachment.InvoiceId);

        var stream = await _storage.OpenReadAsync(attachment.NasRelativePath);
        await _activityLog.LogAsync(
            attachment.InvoiceId,
            InvoiceActivityType.DOWNLOADED,
            actorUserId,
            attachment.WorkflowStepId,
            attachment.FileName);

        return (stream, ContentTypeOf(attachment), attachment.FileName);
    }

    private async Task<IQueryable<InvoiceAttachment>> BuildVisibleQueryAsync(
        InvoiceAccessScope access,
        int? invoiceId)
    {
        if (invoiceId.HasValue)
        {
            await EnsureCanReadInvoiceAsync(invoiceId.Value, access);
        }
        else if (!access.CanAccessAll && access.InvoiceTypeIds.Count == 0)
        {
            return _invoiceDb.InvoiceAttachments.AsNoTracking().Where(_ => false);
        }

        var query = _invoiceDb.InvoiceAttachments.AsNoTracking();
        if (invoiceId.HasValue)
        {
            query = query.Where(a => a.InvoiceId == invoiceId.Value);
        }
        else if (!access.CanAccessAll)
        {
            var typeIds = access.InvoiceTypeIds.ToList();
            query = query.Where(a =>
                a.Invoice.InvoiceTypeId != null &&
                typeIds.Contains(a.Invoice.InvoiceTypeId.Value));
        }

        return query;
    }

    private async Task EnsureCanReadInvoiceAsync(int invoiceId, InvoiceAccessScope? access = null)
    {
        access ??= await _access.ResolveAsync();
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

    private async Task EnsureCanMutateAsync(InvoiceAttachment attachment, int actorUserId)
    {
        if (attachment.UploadedByUserId != actorUserId)
        {
            throw new ForbiddenException("Yalnızca kendi yüklediğiniz eki silebilir veya değiştirebilirsiniz.");
        }

        var openStepId = await _workflow.RequireOpenStepForActorAsync(attachment.InvoiceId, actorUserId);
        if (attachment.WorkflowStepId != openStepId)
        {
            throw new BadRequestException("Tamamlanmış adıma ait belge silinemez veya değiştirilemez.");
        }
    }

    private async Task<InvoiceAttachment> GetRequiredAsync(int id, bool tracking)
    {
        var query = tracking
            ? _invoiceDb.InvoiceAttachments.AsQueryable()
            : _invoiceDb.InvoiceAttachments.AsNoTracking();
        var attachment = await query.FirstOrDefaultAsync(a => a.Id == id);
        if (attachment is null)
        {
            throw new NotFoundException("Ek bulunamadı.");
        }

        return attachment;
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

    private async Task<InvoiceAttachment> SaveNewFileAsync(
        int invoiceId,
        int stepId,
        int actorUserId,
        IFormFile file,
        DateTime now)
    {
        var stored = await StoreFileAsync(invoiceId, stepId, file);
        var attachment = new InvoiceAttachment
        {
            InvoiceId = invoiceId,
            WorkflowStepId = stepId,
            FileName = stored.OriginalName,
            NasRelativePath = stored.RelativePath,
            FileType = stored.FileType,
            FileSizeBytes = (int)file.Length,
            ChecksumSha256 = stored.Checksum,
            UploadedByUserId = actorUserId,
            UploadedAt = now
        };
        _invoiceDb.InvoiceAttachments.Add(attachment);
        return attachment;
    }

    private async Task<(string OriginalName, string RelativePath, string FileType, string Checksum)> StoreFileAsync(
        int invoiceId,
        int? stepId,
        IFormFile file)
    {
        if (file.Length > _storageOptions.MaxFileSizeBytes)
        {
            throw new BadRequestException(
                $"Dosya boyutu en fazla {_storageOptions.MaxFileSizeBytes / (1024 * 1024)} MB olabilir.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "" : file.ContentType;
        if (!AllowedAttachmentTypes.TryGetValue(contentType, out var typeInfo))
        {
            throw new BadRequestException("Yalnızca PDF, PNG, JPEG veya WebP yüklenebilir.");
        }

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName))
        {
            originalName = $"ek{typeInfo.Extension}";
        }

        if (originalName.Length > 255)
        {
            originalName = originalName[..255];
        }

        var stepFolder = stepId.HasValue ? $"steps/{stepId.Value}" : "steps/unassigned";
        var relativePath =
            $"invoices/{DateTime.UtcNow:yyyy}/{invoiceId}/{stepFolder}/{Guid.NewGuid():N}{typeInfo.Extension}";

        string checksum;
        await using (var hashStream = file.OpenReadStream())
        {
            checksum = Convert.ToHexString(await SHA256.HashDataAsync(hashStream));
        }

        await using (var saveStream = file.OpenReadStream())
        {
            await _storage.SaveAsync(relativePath, saveStream);
        }

        return (originalName, relativePath, typeInfo.FileType, checksum);
    }

    private async Task<List<InvoiceAttachmentResponseDto>> MapManyAsync(IReadOnlyList<InvoiceAttachment> attachments)
    {
        var userNames = await _authLookup.GetUserFullNamesAsync(
            attachments.Where(a => a.UploadedByUserId.HasValue).Select(a => a.UploadedByUserId!.Value));

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
                UploadedByUserFullName = a.UploadedByUserId.HasValue
                    && userNames.TryGetValue(a.UploadedByUserId.Value, out var name)
                    ? name
                    : null,
                WorkflowStepId = a.WorkflowStepId,
                UploadedAt = a.UploadedAt
            })
            .ToList();
    }

    private static string ContentTypeOf(InvoiceAttachment attachment)
    {
        var ext = Path.GetExtension(attachment.NasRelativePath);
        return ext.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
