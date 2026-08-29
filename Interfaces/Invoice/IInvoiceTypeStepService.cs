using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceTypeStepService
{
    Task<IReadOnlyList<InvoiceTypeStepResponseDto>> GetByInvoiceTypeIdAsync(int invoiceTypeId);
    Task<InvoiceTypeStepResponseDto> GetByIdAsync(int id);
    Task<InvoiceTypeStepResponseDto> CreateAsync(int invoiceTypeId, CreateInvoiceTypeStepRequestDto request);
    Task<InvoiceTypeStepResponseDto> UpdateAsync(int id, UpdateInvoiceTypeStepRequestDto request);
    Task<InvoiceTypeStepResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
}
