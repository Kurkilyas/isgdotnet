namespace InvoiceTrackingSystemBackend.Constants;

/// <summary>Sabit süreç adımının türü: normal adım mı, departman zinciri yer tutucusu mu.</summary>
public enum StepKind
{
    Fixed,
    DepartmentChainPlaceholder
}
