namespace InvoiceTrackingSystemBackend.Enums;

/// <summary>Durum değiştirmeyen, salt gözlemsel kullanıcı etkileşim türü.</summary>
public enum ActivityType
{
    Viewed,
    OpenedAttachment,
    Downloaded,
    Commented,
    StatusChecked
}
