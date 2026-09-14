using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoices")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoiceWorkflowService _workflowService;

    public InvoicesController(IInvoiceService invoiceService, IInvoiceWorkflowService workflowService)
    {
        _invoiceService = invoiceService;
        _workflowService = workflowService;
    }

    [HttpGet]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? invoiceTypeId = null,
        [FromQuery] bool? isDuplicate = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        var result = await _invoiceService.GetListAsync(
            page, pageSize, search, status, supplierId, invoiceTypeId, isDuplicate, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetAll([FromQuery] string? name = null)
    {
        var result = await _invoiceService.GetAllAsync(name);
        return Ok(result);
    }

    [HttpGet("summary")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetMySummary()
    {
        var result = await _invoiceService.GetMySummaryAsync(CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpGet("inbox")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetInbox(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var result = await _invoiceService.GetInboxAsync(CurrentUserHelper.GetUserId(User), page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _invoiceService.GetByIdAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpPost]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequestDto request)
    {
        var result = await _invoiceService.CreateAsync(request, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceRequestDto request)
    {
        var result = await _invoiceService.UpdateAsync(id, request, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("InvoiceDelete", "AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return Ok(new { message = "Fatura silindi." });
    }

    [HttpPost("{id:int}/archive")]
    [Permission("InvoiceArchive")]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _invoiceService.ArchiveAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpGet("{id:int}/relations")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetRelations(int id)
    {
        var result = await _invoiceService.GetRelationsAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:int}/relations")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> CreateRelation(int id, [FromBody] CreateInvoiceRelationRequestDto request)
    {
        var result = await _invoiceService.CreateRelationAsync(id, request, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpGet("{id:int}/workflow-steps")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetWorkflowSteps(int id)
    {
        var result = await _invoiceService.GetWorkflowStepsAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:int}/workflow/approve")]
    [Permission("InvoiceApprover")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveStepRequestDto request)
    {
        await _workflowService.ApproveAsync(id, CurrentUserHelper.GetUserId(User), request);
        var result = await _invoiceService.GetByIdAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpPost("{id:int}/workflow/reject")]
    [Permission("InvoiceApprover")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectStepRequestDto request)
    {
        await _workflowService.RejectAsync(id, CurrentUserHelper.GetUserId(User), request);
        var result = await _invoiceService.GetByIdAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpPost("{id:int}/workflow/return")]
    [Permission("InvoiceApprover")]
    public async Task<IActionResult> Return(int id, [FromBody] ReturnStepRequestDto request)
    {
        await _workflowService.ReturnAsync(id, CurrentUserHelper.GetUserId(User), request);
        var result = await _invoiceService.GetByIdAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }

    [HttpPost("{id:int}/workflow/missing-document")]
    [Permission("InvoiceApprover")]
    public async Task<IActionResult> FlagMissingDocument(int id, [FromBody] FlagMissingDocumentRequestDto request)
    {
        await _workflowService.FlagMissingDocumentAsync(id, CurrentUserHelper.GetUserId(User), request);
        var result = await _invoiceService.GetByIdAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(result);
    }
}
