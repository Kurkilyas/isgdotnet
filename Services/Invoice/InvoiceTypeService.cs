using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceTypeService : IInvoiceTypeService
{
    private readonly InvoiceDbContext _invoiceDb;
    private readonly AuthReferenceLookup _authLookup;

    public InvoiceTypeService(InvoiceDbContext invoiceDb, AuthReferenceLookup authLookup)
    {
        _invoiceDb = invoiceDb;
        _authLookup = authLookup;
    }

    public async Task<PagedResult<InvoiceTypeListDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _invoiceDb.InvoiceTypes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Code.Contains(term) || t.Name.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InvoiceTypeListDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                IsActive = t.IsActive,
                StepCount = t.Steps.Count,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return PagedResult<InvoiceTypeListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null)
    {
        var query = _invoiceDb.InvoiceTypes.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new IdNameDto
            {
                Id = t.Id,
                Name = t.Name
            })
            .ToListAsync();
    }

    public async Task<InvoiceTypeResponseDto> GetByIdAsync(int id)
    {
        var type = await LoadTypeWithStepsAsync(id, asNoTracking: true);
        return await MapDetailAsync(type);
    }

    public async Task<InvoiceTypeResponseDto> CreateAsync(CreateInvoiceTypeRequestDto request)
    {
        var code = NormalizeCode(request.Code);
        await EnsureCodeAvailableAsync(code);

        var now = DateTime.UtcNow;
        var type = new InvoiceType
        {
            Code = code,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = now
        };
        _invoiceDb.InvoiceTypes.Add(type);
        await _invoiceDb.SaveChangesAsync();

        return await MapDetailAsync(type);
    }

    public async Task<InvoiceTypeResponseDto> UpdateAsync(int id, UpdateInvoiceTypeRequestDto request)
    {
        var type = await GetRequiredAsync(id);
        type.Name = request.Name.Trim();
        type.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<InvoiceTypeResponseDto> SetActiveAsync(int id, SetActiveRequestDto request)
    {
        var type = await GetRequiredAsync(id);
        if (type.IsActive == request.IsActive)
        {
            return await GetByIdAsync(id);
        }

        type.IsActive = request.IsActive;
        type.UpdatedAt = DateTime.UtcNow;
        await _invoiceDb.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var type = await _invoiceDb.InvoiceTypes
            .Include(t => t.Steps)
            .ThenInclude(s => s.Approvers)
            .Include(t => t.SupplierInvoiceTypes)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            throw new NotFoundException("Fatura türü bulunamadı.");
        }

        var now = DateTime.UtcNow;
        type.IsActive = false;
        type.DeletedAt = now;
        type.UpdatedAt = now;
        type.Code = UniqueDeletedCode(type.Id);

        foreach (var step in type.Steps)
        {
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
        }

        foreach (var link in type.SupplierInvoiceTypes)
        {
            link.IsActive = false;
            link.DeletedAt = now;
            link.UpdatedAt = now;
        }

        await _invoiceDb.SaveChangesAsync();
    }

    private async Task<InvoiceType> GetRequiredAsync(int id)
    {
        var type = await _invoiceDb.InvoiceTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            throw new NotFoundException("Fatura türü bulunamadı.");
        }

        return type;
    }

    private async Task<InvoiceType> LoadTypeWithStepsAsync(int id, bool asNoTracking)
    {
        IQueryable<InvoiceType> query = _invoiceDb.InvoiceTypes
            .Include(t => t.Steps)
            .ThenInclude(s => s.Approvers);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var type = await query.FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            throw new NotFoundException("Fatura türü bulunamadı.");
        }

        return type;
    }

    private async Task<InvoiceTypeResponseDto> MapDetailAsync(InvoiceType type)
    {
        var departmentIds = type.Steps
            .Where(s => s.DepartmentId.HasValue)
            .Select(s => s.DepartmentId!.Value);
        var userIds = type.Steps.SelectMany(s => s.Approvers).Select(a => a.UserId);
        var departmentNames = await _authLookup.GetDepartmentNamesAsync(departmentIds);
        var userNames = await _authLookup.GetUserFullNamesAsync(userIds);

        return new InvoiceTypeResponseDto
        {
            Id = type.Id,
            Code = type.Code,
            Name = type.Name,
            IsActive = type.IsActive,
            CreatedAt = type.CreatedAt,
            UpdatedAt = type.UpdatedAt,
            Steps = type.Steps
                .OrderBy(s => s.StepOrder)
                .Select(s => InvoiceMasterDataMapper.MapStep(s, departmentNames, userNames))
                .ToList()
        };
    }

    private async Task EnsureCodeAvailableAsync(string code)
    {
        var exists = await _invoiceDb.InvoiceTypes.AnyAsync(t => t.Code == code);
        if (exists)
        {
            throw new ConflictException("Bu fatura türü kodu zaten kullanılıyor.");
        }
    }

    private static string NormalizeCode(string code)
    {
        var trimmed = code.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("Fatura türü kodu boş olamaz.");
        }

        return trimmed.ToUpperInvariant();
    }

    private static string UniqueDeletedCode(int id)
    {
        var marker = $"DEL{id}";
        return marker.Length <= 30 ? marker : marker[..30];
    }
}
