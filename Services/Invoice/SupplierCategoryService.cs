using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class SupplierCategoryService : ISupplierCategoryService
{
    private readonly InvoiceDbContext _invoiceDb;

    public SupplierCategoryService(InvoiceDbContext invoiceDb)
    {
        _invoiceDb = invoiceDb;
    }

    public async Task<PagedResult<SupplierCategoryResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.SupplierCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = categories.Select(Map).ToList();
        return PagedResult<SupplierCategoryResponseDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null)
    {
        var query = _invoiceDb.SupplierCategories.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new IdNameDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
    }

    public async Task<SupplierCategoryResponseDto> GetByIdAsync(int id)
    {
        return Map(await GetRequiredAsync(id, asNoTracking: true));
    }

    public async Task<SupplierCategoryResponseDto> CreateAsync(CreateSupplierCategoryRequestDto request)
    {
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name);

        var category = new SupplierCategory
        {
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _invoiceDb.SupplierCategories.Add(category);
        await _invoiceDb.SaveChangesAsync();

        return Map(category);
    }

    public async Task<SupplierCategoryResponseDto> UpdateAsync(int id, UpdateSupplierCategoryRequestDto request)
    {
        var category = await GetRequiredAsync(id);
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, excludeId: id);

        category.Name = name;
        category.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return Map(category);
    }

    public async Task<SupplierCategoryResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var category = await GetRequiredAsync(id);
        if (category.IsActive == request.IsActive)
        {
            return Map(category);
        }

        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return Map(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await GetRequiredAsync(id);
        var now = DateTime.UtcNow;
        category.IsActive = false;
        category.DeletedAt = now;
        category.UpdatedAt = now;
        await _invoiceDb.SaveChangesAsync();
    }

    private async Task<SupplierCategory> GetRequiredAsync(int id, bool asNoTracking = false)
    {
        IQueryable<SupplierCategory> query = _invoiceDb.SupplierCategories;
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var category = await query.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            throw new NotFoundException("Tedarikçi kategorisi bulunamadı.");
        }

        return category;
    }

    private async Task EnsureNameAvailableAsync(string name, int? excludeId = null)
    {
        var exists = await _invoiceDb.SupplierCategories.AnyAsync(c =>
            c.Name == name && (!excludeId.HasValue || c.Id != excludeId.Value));
        if (exists)
        {
            throw new ConflictException("Bu kategori adı zaten kullanılıyor.");
        }
    }

    private static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("Kategori adı boş olamaz.");
        }

        return trimmed;
    }

    private static SupplierCategoryResponseDto Map(SupplierCategory category)
    {
        return new SupplierCategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };
    }
}
