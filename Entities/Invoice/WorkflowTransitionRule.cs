using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;
using InvoiceTrackingSystemBackend.Enums;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. "Hangi tetikleyicide hangi adıma dönülür" kuralını KOD DEĞİŞTİRMEDEN yönetilebilir hale getirir.
/// Ör: (trigger=MISSING_DOCUMENT_FLAGGED, source=NULL, target=ACCOUNTING).
/// </summary>
public class WorkflowTransitionRule : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public WorkflowActionType TriggerAction { get; set; }

    /// <summary>Bu kural HANGİ adımda tetiklenirse geçerli. NULL ise her yerde geçerli genel kural.</summary>
    [MaxLength(30)]
    public string? SourceStepCode { get; set; }
    public WorkflowStepDefinition? SourceStep { get; set; }

    /// <summary>Tetiklenince GİDİLECEK adımın kodu.</summary>
    [MaxLength(30)]
    public string TargetStepCode { get; set; } = null!;
    public WorkflowStepDefinition TargetStep { get; set; } = null!;

    /// <summary>Birden fazla kural eşleşirse hangisi önce uygulanır (küçük sayı = önce).</summary>
    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
