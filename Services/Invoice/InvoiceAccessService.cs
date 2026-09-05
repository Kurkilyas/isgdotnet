using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

public class InvoiceAccessService : IInvoiceAccessService
{
    private const string HttpScopeKey = "InvoiceAccessScope";

    private readonly InvoiceDbContext _invoiceDb;
    private readonly UserDbContext _userDb;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoiceAccessService(
        InvoiceDbContext invoiceDb,
        UserDbContext userDb,
        IHttpContextAccessor httpContextAccessor)
    {
        _invoiceDb = invoiceDb;
        _userDb = userDb;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<InvoiceAccessScope> ResolveAsync()
    {
        var http = _httpContextAccessor.HttpContext
                   ?? throw new InvalidOperationException("Kullanıcı oturumu yok.");

        if (http.Items.TryGetValue(HttpScopeKey, out var cached) && cached is InvoiceAccessScope scope)
        {
            return scope;
        }

        var userId = CurrentUserHelper.GetUserId(http.User);
        var permissions = CurrentUserHelper.GetPermissions(http.User);
        var canAccessAll = permissions.Contains("AdminRead") || permissions.Contains("AdminWrite");
        var canMutateAll = permissions.Contains("AdminWrite");

        if (canAccessAll)
        {
            scope = new InvoiceAccessScope
            {
                CanAccessAll = true,
                CanMutateAll = canMutateAll,
                InvoiceTypeIds = new HashSet<int>()
            };
            http.Items[HttpScopeKey] = scope;
            return scope;
        }

        var typeIds = await GetAccessibleInvoiceTypeIdsAsync(userId);
        scope = new InvoiceAccessScope
        {
            CanAccessAll = false,
            CanMutateAll = false,
            InvoiceTypeIds = typeIds
        };
        http.Items[HttpScopeKey] = scope;
        return scope;
    }

    private async Task<IReadOnlySet<int>> GetAccessibleInvoiceTypeIdsAsync(int userId)
    {
        var departmentIds = await GetRoleDepartmentIdsAsync(userId);

        var fromApprovers = await _invoiceDb.InvoiceTypeStepApprovers
            .AsNoTracking()
            .Where(a =>
                a.IsActive &&
                a.UserId == userId &&
                a.InvoiceTypeStep.IsActive &&
                a.InvoiceTypeStep.InvoiceType.IsActive)
            .Select(a => a.InvoiceTypeStep.InvoiceTypeId)
            .ToListAsync();

        List<int> fromDepartments = [];
        if (departmentIds.Count > 0)
        {
            fromDepartments = await _invoiceDb.InvoiceTypeSteps
                .AsNoTracking()
                .Where(s =>
                    s.IsActive &&
                    s.InvoiceType.IsActive &&
                    s.DepartmentId != null &&
                    departmentIds.Contains(s.DepartmentId.Value))
                .Select(s => s.InvoiceTypeId)
                .ToListAsync();
        }

        return fromApprovers.Concat(fromDepartments).ToHashSet();
    }

    private async Task<List<int>> GetRoleDepartmentIdsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return await _userDb.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive &&
                ur.Role.DepartmentId != null)
            .Select(ur => ur.Role.DepartmentId!.Value)
            .Distinct()
            .ToListAsync();
    }
}
