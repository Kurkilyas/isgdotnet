namespace InvoiceTrackingSystemBackend.Entities.Common;

/// <summary>Oluşturulma/güncellenme zamanı takip edilen entity'ler.</summary>
public interface IAuditableEntity
{
    DateTime? CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
