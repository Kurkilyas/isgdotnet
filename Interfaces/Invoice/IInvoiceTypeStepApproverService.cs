using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceTypeStepApproverService
{
    Task<IReadOnlyList<InvoiceTypeStepApproverResponseDto>> GetByStepIdAsync(int invoiceTypeStepId);
    Task<InvoiceTypeStepApproverResponseDto> CreateAsync(int invoiceTypeStepId, CreateInvoiceTypeStepApproverRequestDto request);
    Task<InvoiceTypeStepApproverResponseDto> UpdateAsync(int id, UpdateInvoiceTypeStepApproverRequestDto request);
    Task<InvoiceTypeStepApproverResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
}
