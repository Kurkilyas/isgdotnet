using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceNotificationLogService : IInvoiceNotificationLogService
{
    private readonly InvoiceDbContext _invoiceDb;

    public InvoiceNotificationLogService(InvoiceDbContext invoiceDb)
    {
        _invoiceDb = invoiceDb;
    }

    public async Task LogAsync(
        int invoiceId,
        string recipientEmail,
        NotificationType notificationType,
        int? recipientUserId = null,
        int? workflowStepId = null,
        bool isSuccess = true,
        string? errorMessage = null)
    {
        var invoiceExists = await _invoiceDb.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.Id == invoiceId);
        if (!invoiceExists)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        var email = recipientEmail.Trim();
        if (email.Length > 150)
        {
            email = email[..150];
        }

        var trimmedError = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        if (trimmedError is { Length: > 500 })
        {
            trimmedError = trimmedError[..500];
        }

        _invoiceDb.InvoiceNotificationLogs.Add(new InvoiceNotificationLog
        {
            InvoiceId = invoiceId,
            WorkflowStepId = workflowStepId,
            RecipientUserId = recipientUserId,
            RecipientEmail = email,
            NotificationType = notificationType,
            SentAt = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = trimmedError
        });

        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<PagedResult<InvoiceNotificationLogResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? recipientUserId = null,
        NotificationType? notificationType = null,
        bool? isSuccess = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.InvoiceNotificationLogs.AsNoTracking();

        if (invoiceId.HasValue)
        {
            query = query.Where(e => e.InvoiceId == invoiceId.Value);
        }

        if (recipientUserId.HasValue)
        {
            query = query.Where(e => e.RecipientUserId == recipientUserId.Value);
        }

        if (notificationType.HasValue)
        {
            query = query.Where(e => e.NotificationType == notificationType.Value);
        }

        if (isSuccess.HasValue)
        {
            query = query.Where(e => e.IsSuccess == isSuccess.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.SentAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new InvoiceNotificationLogResponseDto
            {
                Id = e.Id,
                InvoiceId = e.InvoiceId,
                WorkflowStepId = e.WorkflowStepId,
                RecipientUserId = e.RecipientUserId,
                RecipientEmail = e.RecipientEmail,
                NotificationType = e.NotificationType,
                SentAt = e.SentAt,
                IsSuccess = e.IsSuccess,
                ErrorMessage = e.ErrorMessage
            })
            .ToListAsync();

        return PagedResult<InvoiceNotificationLogResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<InvoiceNotificationLogResponseDto?> GetByIdAsync(int id)
    {
        return await _invoiceDb.InvoiceNotificationLogs
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new InvoiceNotificationLogResponseDto
            {
                Id = e.Id,
                InvoiceId = e.InvoiceId,
                WorkflowStepId = e.WorkflowStepId,
                RecipientUserId = e.RecipientUserId,
                RecipientEmail = e.RecipientEmail,
                NotificationType = e.NotificationType,
                SentAt = e.SentAt,
                IsSuccess = e.IsSuccess,
                ErrorMessage = e.ErrorMessage
            })
            .FirstOrDefaultAsync();
    }
}
