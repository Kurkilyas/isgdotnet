using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = InvoiceTrackingSystemBackend.Entities.Invoice.Invoice;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceLineItemService : IInvoiceLineItemService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly IInvoiceAccessService _access;

    public InvoiceLineItemService(InvoiceDbContext invoiceDb, IInvoiceAccessService access)
    {
        _invoiceDb = invoiceDb;
        _access = access;
    }

    public async Task<PagedResult<InvoiceLineItemResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var access = await _access.ResolveAsync();
        var query = await BuildVisibleQueryAsync(access, invoiceId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l => l.Description.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var entities = await query
            .OrderBy(l => l.InvoiceId)
            .ThenBy(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<InvoiceLineItemResponseDto>.Create(
            entities.Select(Map).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(int? invoiceId = null)
    {
        var access = await _access.ResolveAsync();
        var query = await BuildVisibleQueryAsync(access, invoiceId);

        var rows = await query
            .OrderBy(l => l.InvoiceId)
            .ThenBy(l => l.Id)
            .Select(l => new { l.Id, l.Description, l.InvoiceId })
            .ToListAsync();

        return rows.Select(l => new IdNameDto
        {
            Id = l.Id,
            Name = $"{l.Description} #{l.InvoiceId}"
        }).ToList();
    }

    public async Task<InvoiceLineItemResponseDto> GetByIdAsync(int id)
    {
        var line = await GetRequiredAsync(id, tracking: false);
        await EnsureCanReadInvoiceAsync(line.InvoiceId);
        return Map(line);
    }

    public async Task<InvoiceLineItemResponseDto> CreateAsync(CreateInvoiceLineItemRequestDto request)
    {
        if (request.InvoiceId < 1)
        {
            throw new BadRequestException("Geçerli bir fatura seçilmedi.");
        }

        var invoice = await GetRequiredInvoiceForWriteAsync(request.InvoiceId);
        var now = DateTime.UtcNow;
        var line = MapNew(request, now);
        line.InvoiceId = request.InvoiceId;
        _invoiceDb.InvoiceLineItems.Add(line);

        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        return Map(line);
    }

    public async Task<InvoiceLineItemResponseDto> UpdateAsync(int id, UpdateInvoiceLineItemRequestDto request)
    {
        var line = await GetRequiredAsync(id, tracking: true);
        var invoice = await GetRequiredInvoiceForWriteAsync(line.InvoiceId);
        var now = DateTime.UtcNow;
        Apply(line, request.Description, request.Quantity, request.UnitPrice, request.LineAmount);
        line.UpdatedAt = now;

        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
        return Map(line);
    }

    public async Task DeleteAsync(int id)
    {
        var line = await GetRequiredAsync(id, tracking: true);
        var invoice = await GetRequiredInvoiceForWriteAsync(line.InvoiceId);
        var now = DateTime.UtcNow;
        line.DeletedAt = now;
        line.UpdatedAt = now;

        invoice.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
    }

    private async Task<IQueryable<InvoiceLineItem>> BuildVisibleQueryAsync(
        InvoiceAccessScope access,
        int? invoiceId)
    {
        if (invoiceId.HasValue)
        {
            await EnsureCanReadInvoiceAsync(invoiceId.Value, access);
        }
        else if (!access.CanAccessAll && access.InvoiceTypeIds.Count == 0)
        {
            return _invoiceDb.InvoiceLineItems.AsNoTracking().Where(_ => false);
        }

        var query = _invoiceDb.InvoiceLineItems.AsNoTracking();
        if (invoiceId.HasValue)
        {
            query = query.Where(l => l.InvoiceId == invoiceId.Value);
        }
        else if (!access.CanAccessAll)
        {
            var typeIds = access.InvoiceTypeIds.ToList();
            query = query.Where(l =>
                l.Invoice.InvoiceTypeId != null &&
                typeIds.Contains(l.Invoice.InvoiceTypeId.Value));
        }

        return query;
    }

    private async Task EnsureCanReadInvoiceAsync(int invoiceId, InvoiceAccessScope? access = null)
    {
        access ??= await _access.ResolveAsync();
        var typeId = await GetInvoiceTypeIdAsync(invoiceId);
        access.EnsureCanRead(typeId);
    }

    private async Task<InvoiceEntity> GetRequiredInvoiceForWriteAsync(int invoiceId)
    {
        var invoice = await GetRequiredInvoiceAsync(invoiceId);
        (await _access.ResolveAsync()).EnsureCanWrite(invoice.InvoiceTypeId);
        EnsureContentMutable(invoice);
        return invoice;
    }

    private static void EnsureContentMutable(InvoiceEntity invoice)
    {
        if (invoice.IsContentLocked)
        {
            throw new BadRequestException(
                "Departman sürecine girmiş veya tamamlanmış faturanın kalemleri değiştirilemez.");
        }
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

    private async Task<InvoiceEntity> GetRequiredInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceDb.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            throw new NotFoundException("Fatura bulunamadı.");
        }

        return invoice;
    }

    private async Task<InvoiceLineItem> GetRequiredAsync(int id, bool tracking)
    {
        var query = tracking
            ? _invoiceDb.InvoiceLineItems.AsQueryable()
            : _invoiceDb.InvoiceLineItems.AsNoTracking();
        var line = await query.FirstOrDefaultAsync(l => l.Id == id);
        if (line is null)
        {
            throw new NotFoundException("Fatura kalemi bulunamadı.");
        }

        return line;
    }

    private static InvoiceLineItem MapNew(CreateInvoiceLineItemRequestDto request, DateTime now)
    {
        var line = new InvoiceLineItem
        {
            CreatedAt = now
        };
        Apply(line, request.Description, request.Quantity, request.UnitPrice, request.LineAmount);
        return line;
    }

    private static void Apply(
        InvoiceLineItem line,
        string description,
        decimal? quantity,
        decimal? unitPrice,
        decimal? lineAmount)
    {
        var trimmed = description.Trim();
        if (trimmed.Length == 0)
        {
            throw new BadRequestException("Kalem açıklaması boş olamaz.");
        }

        line.Description = trimmed;
        line.Quantity = quantity;
        line.UnitPrice = unitPrice;
        line.LineAmount = lineAmount ?? (quantity.HasValue && unitPrice.HasValue
            ? decimal.Round(quantity.Value * unitPrice.Value, 2, MidpointRounding.AwayFromZero)
            : null);
    }

    private static InvoiceLineItemResponseDto Map(InvoiceLineItem line)
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
}
