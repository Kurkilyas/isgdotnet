using InvoiceTrackingSystemBackend.Exceptions;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceAccessService
{
    Task<InvoiceAccessScope> ResolveAsync();
}

public sealed class InvoiceAccessScope
{
    public bool CanAccessAll { get; init; }
    public bool CanMutateAll { get; init; }
    public IReadOnlySet<int> InvoiceTypeIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<int> DepartmentIds { get; init; } = new HashSet<int>();

    public bool CanRead(int? invoiceTypeId)
    {
        if (CanAccessAll)
        {
            return true;
        }

        return invoiceTypeId.HasValue && InvoiceTypeIds.Contains(invoiceTypeId.Value);
    }

    public bool CanWrite(int? invoiceTypeId)
    {
        if (CanMutateAll)
        {
            return true;
        }

        return invoiceTypeId.HasValue && InvoiceTypeIds.Contains(invoiceTypeId.Value);
    }

    public void EnsureCanRead(int? invoiceTypeId)
    {
        if (!CanRead(invoiceTypeId))
        {
            throw new ForbiddenException("Bu faturayı görüntüleme yetkiniz yok.");
        }
    }

    public void EnsureCanWrite(int? invoiceTypeId)
    {
        if (!CanWrite(invoiceTypeId))
        {
            throw new ForbiddenException("Bu faturada işlem yapma yetkiniz yok.");
        }
    }

    public void EnsureCanCreate(int? invoiceTypeId)
    {
        if (!CanWrite(invoiceTypeId))
        {
            throw new ForbiddenException("Bu fatura türü için kayıt oluşturma yetkiniz yok.");
        }
    }

    public void EnsureCanDelete(int? invoiceTypeId)
    {
        if (!CanWrite(invoiceTypeId))
        {
            throw new ForbiddenException("Bu faturayı silme yetkiniz yok.");
        }
    }
}
