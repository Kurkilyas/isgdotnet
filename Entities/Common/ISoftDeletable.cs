namespace InvoiceTrackingSystemBackend.Entities.Common;

/// <summary>Soft delete uygulanan entity'ler - DeletedAt dolu ise kayıt silinmiş sayılır, fiziksel DELETE yapılmaz.</summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
