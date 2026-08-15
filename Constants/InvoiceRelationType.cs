namespace InvoiceTrackingSystemBackend.Constants;

/// <summary>İki fatura arasındaki ilişkinin türü.</summary>
public enum InvoiceRelationType
{
    DuplicateOf,
    PriceDifferenceOf,
    CreditNoteOf
}
