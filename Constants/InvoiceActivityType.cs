namespace InvoiceTrackingSystemBackend.Constants;

/// <summary>invoice_activity_logs.activity_type — durum değiştirmeyen, salt gözlemsel fatura etkileşimi.</summary>
public enum InvoiceActivityType
{
    VIEWED,
    OPENED_ATTACHMENT,
    DOWNLOADED,
    COMMENTED,
    STATUS_CHECKED,
    ATTACHMENT_UPLOADED,
    ATTACHMENT_UPDATED,
    ATTACHMENT_DELETED
}
