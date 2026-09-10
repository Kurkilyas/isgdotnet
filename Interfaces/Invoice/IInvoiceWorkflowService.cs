using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceWorkflowService
{
    Task EnsureTypeHasActiveStepsAsync(int invoiceTypeId);
    Task StartAsync(int invoiceId, int? actorUserId);
    Task<int> RequireOpenStepForActorAsync(int invoiceId, int actorUserId);
    Task ApproveAsync(int invoiceId, int actorUserId, ApproveStepRequestDto request);
    Task RejectAsync(int invoiceId, int actorUserId, RejectStepRequestDto request);
    Task ReturnAsync(int invoiceId, int actorUserId, ReturnStepRequestDto request);
    Task FlagMissingDocumentAsync(int invoiceId, int actorUserId, FlagMissingDocumentRequestDto request);
}
