using InvoiceTrackingSystemBackend.Entities.Common;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. "Hangi tetikleyicide hangi adıma dönülür" kuralını KOD DEĞİŞTİRMEDEN yönetilebilir hale getirir.
/// Kural artık InvoiceTypeStep'e doğrudan referans verdiği için otomatik olarak tek bir fatura türü kapsamında kalır
/// (source/target aynı InvoiceTypeId'ye ait olmalı - uygulama seviyesinde doğrulanır).
/// Ör: (trigger=MISSING_DOCUMENT_FLAGGED, source=NULL, target=Muhasebe adımı).
/// </summary>
public class WorkflowTransitionRule : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public WorkflowActionType TriggerAction { get; set; }

    /// <summary>Bu kural HANGİ adımda tetiklenirse geçerli. NULL ise bu fatura türü içinde her adımdan tetiklenebilir.</summary>
    public int? SourceStepId { get; set; }
    public InvoiceTypeStep? SourceStep { get; set; }

    /// <summary>Tetiklenince GİDİLECEK adım.</summary>
    public int TargetStepId { get; set; }
    public InvoiceTypeStep TargetStep { get; set; } = null!;

    /// <summary>Birden fazla kural eşleşirse hangisi önce uygulanır (küçük sayı = önce).</summary>
    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
