using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

/// <summary>Invoice DB'deki SoftFK'lar (user_id, department_id) için Auth DB doğrulama ve isim çözümleme.</summary>
public class AuthReferenceLookup
{
    private readonly UserDbContext _userDb;

    public AuthReferenceLookup(UserDbContext userDb)
    {
        _userDb = userDb;
    }

    public async Task EnsureDepartmentExistsAsync(int? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return;
        }

        var exists = await _userDb.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId.Value && d.IsActive);
        if (!exists)
        {
            throw new BadRequestException("Geçerli bir departman seçilmedi.");
        }
    }

    public async Task EnsureUserExistsAsync(int userId)
    {
        var exists = await _userDb.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.IsActive);
        if (!exists)
        {
            throw new BadRequestException("Geçerli bir kullanıcı seçilmedi.");
        }
    }

    public async Task EnsureUsersExistAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var foundCount = await _userDb.Users
            .AsNoTracking()
            .CountAsync(u => ids.Contains(u.Id) && u.IsActive);
        if (foundCount != ids.Count)
        {
            throw new BadRequestException("Onaylayıcı listesinde geçersiz veya pasif kullanıcı var.");
        }
    }

    public async Task<Dictionary<int, string>> GetDepartmentNamesAsync(IEnumerable<int> departmentIds)
    {
        var ids = departmentIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _userDb.Departments
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name);
    }

    public async Task<Dictionary<int, string>> GetUserFullNamesAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _userDb.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
    }
}
