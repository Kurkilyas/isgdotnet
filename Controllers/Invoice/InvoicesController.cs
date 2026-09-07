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

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
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
    public async Task<IActionResult> GetAll()
    {
        var result = await _invoiceService.GetAllAsync();
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
        var result = await _invoiceService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("InvoiceDelete", "AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoiceService.DeleteAsync(id);
        return Ok(new { message = "Fatura silindi." });
    }

    [HttpGet("{id:int}/line-items")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetLineItems(int id)
    {
        var result = await _invoiceService.GetLineItemsAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:int}/line-items")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> CreateLineItem(int id, [FromBody] CreateInvoiceLineItemRequestDto request)
    {
        var result = await _invoiceService.CreateLineItemAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/line-items/{lineItemId:int}")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> UpdateLineItem(int id, int lineItemId, [FromBody] UpdateInvoiceLineItemRequestDto request)
    {
        var result = await _invoiceService.UpdateLineItemAsync(id, lineItemId, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}/line-items/{lineItemId:int}")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> DeleteLineItem(int id, int lineItemId)
    {
        await _invoiceService.DeleteLineItemAsync(id, lineItemId);
        return Ok(new { message = "Fatura kalemi silindi." });
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

    [HttpGet("{id:int}/attachments")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetAttachments(int id)
    {
        var result = await _invoiceService.GetAttachmentsAsync(id);
        return Ok(result);
    }
}
