using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IWorkflowTransitionRuleService
{
    Task<PagedResult<WorkflowTransitionRuleResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceTypeId = null,
        WorkflowActionType? triggerAction = null,
        bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null);
    Task<IReadOnlyList<WorkflowTransitionRuleResponseDto>> GetByInvoiceTypeIdAsync(int invoiceTypeId);
    Task<WorkflowTransitionRuleResponseDto> GetByIdAsync(int id);
    Task<WorkflowTransitionRuleResponseDto> CreateAsync(int invoiceTypeId, CreateWorkflowTransitionRuleRequestDto request);
    Task<WorkflowTransitionRuleResponseDto> UpdateAsync(int id, UpdateWorkflowTransitionRuleRequestDto request);
    Task<WorkflowTransitionRuleResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
}
