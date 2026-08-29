using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class SupplierService : ISupplierService
{
    private readonly InvoiceDbContext _invoiceDb;

    public SupplierService(InvoiceDbContext invoiceDb)
    {
        _invoiceDb = invoiceDb;
    }

    public async Task<PagedResult<SupplierListDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null,
        int? supplierCategoryId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.Suppliers
            .AsNoTracking()
            .Include(s => s.SupplierCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.VknTckn.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (supplierCategoryId.HasValue)
        {
            query = query.Where(s => s.SupplierCategoryId == supplierCategoryId.Value);
        }

        var totalCount = await query.CountAsync();
        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = suppliers.Select(MapList).ToList();
        return PagedResult<SupplierListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync()
    {
        return await _invoiceDb.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new IdNameDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();
    }

    public async Task<SupplierResponseDto> GetByIdAsync(int id)
    {
        var supplier = await _invoiceDb.Suppliers
            .AsNoTracking()
            .Include(s => s.SupplierCategory)
            .Include(s => s.SupplierInvoiceTypes)
            .ThenInclude(sit => sit.InvoiceType)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
        {
            throw new NotFoundException("Tedarikçi bulunamadı.");
        }

        return MapDetail(supplier);
    }

    public async Task<SupplierResponseDto> CreateAsync(CreateSupplierRequestDto request)
    {
        var vkn = NormalizeVkn(request.VknTckn);
        await EnsureVknAvailableAsync(vkn);
        await EnsureCategoryExistsAsync(request.SupplierCategoryId);

        var supplier = new Supplier
        {
            VknTckn = vkn,
            Name = request.Name.Trim(),
            SupplierCategoryId = request.SupplierCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _invoiceDb.Suppliers.Add(supplier);
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(supplier.Id);
    }

    public async Task<SupplierResponseDto> UpdateAsync(int id, UpdateSupplierRequestDto request)
    {
        var supplier = await GetRequiredAsync(id);
        await EnsureCategoryExistsAsync(request.SupplierCategoryId);

        supplier.Name = request.Name.Trim();
        supplier.SupplierCategoryId = request.SupplierCategoryId;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<SupplierResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var supplier = await GetRequiredAsync(id);
        if (supplier.IsActive == request.IsActive)
        {
            return await GetByIdAsync(id);
        }

        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await _invoiceDb.Suppliers
            .Include(s => s.SupplierInvoiceTypes)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
        {
            throw new NotFoundException("Tedarikçi bulunamadı.");
        }

        var now = DateTime.UtcNow;
        supplier.IsActive = false;
        supplier.DeletedAt = now;
        supplier.UpdatedAt = now;
        supplier.VknTckn = UniqueDeletedVkn(supplier.Id);

        foreach (var link in supplier.SupplierInvoiceTypes)
        {
            link.IsActive = false;
            link.DeletedAt = now;
            link.UpdatedAt = now;
        }

        await _invoiceDb.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<SupplierInvoiceTypeResponseDto>> GetInvoiceTypesAsync(int supplierId)
    {
        await EnsureSupplierExistsAsync(supplierId);

        var links = await _invoiceDb.SupplierInvoiceTypes
            .AsNoTracking()
            .Include(sit => sit.Supplier)
            .Include(sit => sit.InvoiceType)
            .Where(sit => sit.SupplierId == supplierId)
            .OrderBy(sit => sit.InvoiceType.Name)
            .ToListAsync();

        return links.Select(MapLink).ToList();
    }

    public async Task<IReadOnlyList<SupplierInvoiceTypeResponseDto>> AssignInvoiceTypesAsync(
        int supplierId,
        AssignSupplierInvoiceTypesRequestDto request)
    {
        var supplier = await GetRequiredAsync(id: supplierId);
        if (!supplier.IsActive)
        {
            throw new BadRequestException("Pasif bir tedarikçiye fatura türü atanamaz.");
        }

        var invoiceTypeIds = request.InvoiceTypeIds.Distinct().ToList();
        var types = await _invoiceDb.InvoiceTypes
            .Where(t => invoiceTypeIds.Contains(t.Id))
            .ToListAsync();
        if (types.Count != invoiceTypeIds.Count)
        {
            throw new NotFoundException("Fatura türlerinden biri bulunamadı.");
        }

        if (types.Any(t => !t.IsActive))
        {
            throw new BadRequestException("Pasif bir fatura türü tedarikçiye atanamaz.");
        }

        var existing = await _invoiceDb.SupplierInvoiceTypes
            .IgnoreQueryFilters()
            .Where(sit => sit.SupplierId == supplierId && invoiceTypeIds.Contains(sit.InvoiceTypeId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var typeId in invoiceTypeIds)
        {
            var row = existing.FirstOrDefault(sit => sit.InvoiceTypeId == typeId);
            if (row is null)
            {
                _invoiceDb.SupplierInvoiceTypes.Add(new SupplierInvoiceType
                {
                    SupplierId = supplierId,
                    InvoiceTypeId = typeId,
                    IsActive = true,
                    CreatedAt = now
                });
                continue;
            }

            if (row.DeletedAt is not null || !row.IsActive)
            {
                row.DeletedAt = null;
                row.IsActive = true;
                row.UpdatedAt = now;
                row.CreatedAt ??= now;
            }
        }

        await _invoiceDb.SaveChangesAsync();
        return await GetInvoiceTypesAsync(supplierId);
    }

    public async Task RevokeInvoiceTypeAsync(int supplierId, int invoiceTypeId)
    {
        await EnsureSupplierExistsAsync(supplierId);

        var link = await _invoiceDb.SupplierInvoiceTypes
            .FirstOrDefaultAsync(sit => sit.SupplierId == supplierId && sit.InvoiceTypeId == invoiceTypeId);
        if (link is null)
        {
            throw new NotFoundException("Tedarikçide bu fatura türü bulunamadı.");
        }

        var now = DateTime.UtcNow;
        link.IsActive = false;
        link.DeletedAt = now;
        link.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
    }

    private async Task<Supplier> GetRequiredAsync(int id)
    {
        var supplier = await _invoiceDb.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
        {
            throw new NotFoundException("Tedarikçi bulunamadı.");
        }

        return supplier;
    }

    private async Task EnsureSupplierExistsAsync(int supplierId)
    {
        var exists = await _invoiceDb.Suppliers.AsNoTracking().AnyAsync(s => s.Id == supplierId);
        if (!exists)
        {
            throw new NotFoundException("Tedarikçi bulunamadı.");
        }
    }

    private async Task EnsureVknAvailableAsync(string vkn)
    {
        var exists = await _invoiceDb.Suppliers.AnyAsync(s => s.VknTckn == vkn);
        if (exists)
        {
            throw new ConflictException("Bu VKN/TCKN zaten kayıtlı.");
        }
    }

    private async Task EnsureCategoryExistsAsync(int? supplierCategoryId)
    {
        if (!supplierCategoryId.HasValue)
        {
            return;
        }

        var exists = await _invoiceDb.SupplierCategories
            .AsNoTracking()
            .AnyAsync(c => c.Id == supplierCategoryId.Value && c.IsActive);
        if (!exists)
        {
            throw new BadRequestException("Geçerli bir tedarikçi kategorisi seçilmedi.");
        }
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

    private static string UniqueDeletedVkn(int id)
    {
        return "D" + id.ToString("D10");
    }

    private static SupplierListDto MapList(Supplier supplier)
    {
        return new SupplierListDto
        {
            Id = supplier.Id,
            VknTckn = supplier.VknTckn,
            Name = supplier.Name,
            SupplierCategoryId = supplier.SupplierCategoryId,
            SupplierCategoryName = supplier.SupplierCategory?.Name,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt
        };
    }

    private static SupplierResponseDto MapDetail(Supplier supplier)
    {
        return new SupplierResponseDto
        {
            Id = supplier.Id,
            VknTckn = supplier.VknTckn,
            Name = supplier.Name,
            SupplierCategoryId = supplier.SupplierCategoryId,
            SupplierCategoryName = supplier.SupplierCategory?.Name,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt,
            InvoiceTypes = supplier.SupplierInvoiceTypes
                .Where(sit => sit.IsActive && sit.InvoiceType is not null)
                .Select(sit => InvoiceMasterDataMapper.MapSummary(sit.InvoiceType))
                .ToList()
        };
    }

    private static SupplierInvoiceTypeResponseDto MapLink(SupplierInvoiceType link)
    {
        return new SupplierInvoiceTypeResponseDto
        {
            Id = link.Id,
            SupplierId = link.SupplierId,
            SupplierName = link.Supplier.Name,
            InvoiceTypeId = link.InvoiceTypeId,
            InvoiceTypeCode = link.InvoiceType.Code,
            InvoiceTypeName = link.InvoiceType.Name,
            IsActive = link.IsActive
        };
    }
}
