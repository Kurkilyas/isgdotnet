namespace InvoiceTrackingSystemBackend.Constants;

/// <summary>Faturanın o anki genel durumu (dashboard/filtreleme için).</summary>
public enum InvoiceStatus
{
    Received,
    PendingErpCheck,
    ErrorReturned,
    PendingAssignment,
    InDepartmentChain,
    PendingAuditor1,
    PendingAuditor2,
    PendingAccounting,
    Archived,
    Completed,
    Rejected
}
