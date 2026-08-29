using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Entities.Invoice;

namespace InvoiceTrackingSystemBackend.Services.Invoice;

internal static class InvoiceMasterDataMapper
{
    public static InvoiceTypeSummaryDto MapSummary(InvoiceType type)
    {
        return new InvoiceTypeSummaryDto
        {
            Id = type.Id,
            Code = type.Code,
            Name = type.Name,
            IsActive = type.IsActive
        };
    }

    public static InvoiceTypeStepResponseDto MapStep(
        InvoiceTypeStep step,
        IReadOnlyDictionary<int, string> departmentNames,
        IReadOnlyDictionary<int, string> userNames)
    {
        string? departmentName = null;
        if (step.DepartmentId.HasValue)
        {
            departmentNames.TryGetValue(step.DepartmentId.Value, out departmentName);
        }

        return new InvoiceTypeStepResponseDto
        {
            Id = step.Id,
            InvoiceTypeId = step.InvoiceTypeId,
            StepOrder = step.StepOrder,
            StepName = step.StepName,
            StepRoleTag = step.StepRoleTag,
            DepartmentId = step.DepartmentId,
            DepartmentName = departmentName,
            MaxDurationDays = step.MaxDurationDays,
            IsActive = step.IsActive,
            Approvers = step.Approvers
                .OrderBy(a => a.Priority)
                .Select(a => MapApprover(a, userNames))
                .ToList()
        };
    }

    public static InvoiceTypeStepApproverResponseDto MapApprover(
        InvoiceTypeStepApprover approver,
        IReadOnlyDictionary<int, string> userNames)
    {
        userNames.TryGetValue(approver.UserId, out var fullName);
        return new InvoiceTypeStepApproverResponseDto
        {
            Id = approver.Id,
            InvoiceTypeStepId = approver.InvoiceTypeStepId,
            UserId = approver.UserId,
            UserFullName = fullName,
            Priority = approver.Priority,
            IsActive = approver.IsActive
        };
    }
}
