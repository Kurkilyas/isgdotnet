using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. Kişiye özel adımlarda (InvoiceTypeStep.DepartmentId NULL) onaylayabilecek
/// aday(lar)ı öncelik sırasıyla listeler. Atama motoru Priority ASC sırayla
/// User.IsOutOfOffice = false olan İLK adayı InvoiceWorkflowStep.AssignedUserId yapar.
/// Zincirdeki HERKES aynı anda müsait değilse, AssignedUserId NULL bırakılıp
/// InvoiceTypeStep.DepartmentId üzerinden o departmanın yöneticisine düşer.
/// </summary>
public class InvoiceTypeStepApprover : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int InvoiceTypeStepId { get; set; }
    public InvoiceTypeStep InvoiceTypeStep { get; set; } = null!;

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int UserId { get; set; }

    /// <summary>1 = asıl/ilk bakan, 2 = 1 numara müsait değilse devreye giren, 3 = sıradaki...</summary>
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
