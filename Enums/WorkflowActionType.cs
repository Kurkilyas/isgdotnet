namespace InvoiceTrackingSystemBackend.Enums;

/// <summary>Workflow geçmişi action_type ve transition kurallarındaki trigger_action için ortak değer kümesi.</summary>
public enum WorkflowActionType
{
    StatusChanged,
    Assigned,
    Approved,
    MissingDocumentFlagged,
    Auditor1Rejected,
    Auditor2Rejected,
    Rejected,
    Returned,
    Completed,
    Archived
}
