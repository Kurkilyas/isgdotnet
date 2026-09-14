using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-attachments")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceAttachmentsController : ControllerBase
{
    private readonly IInvoiceAttachmentService _service;

    public InvoiceAttachmentsController(IInvoiceAttachmentService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceId = null,
        [FromQuery] int? workflowStepId = null,
        [FromQuery] int? uploadedByUserId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetListAsync(
            page, pageSize, invoiceId, workflowStepId, uploadedByUserId, search);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetAll([FromQuery] int? invoiceId = null, [FromQuery] string? name = null)
    {
        var result = await _service.GetAllAsync(invoiceId, name);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:int}/content")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetContent(int id)
    {
        var (content, contentType, fileName) = await _service.OpenContentAsync(
            id, CurrentUserHelper.GetUserId(User));
        return File(content, contentType, fileName);
    }

    [HttpPost]
    [Permission("InvoiceApprover")]
    [RequestSizeLimit(20 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> Create(
        [FromForm] int invoiceId,
        [FromForm] List<IFormFile> files)
    {
        var result = await _service.CreateAsync(invoiceId, CurrentUserHelper.GetUserId(User), files);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("InvoiceApprover")]
    [RequestSizeLimit(20 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateInvoiceAttachmentRequestDto request,
        [FromForm] IFormFile? file)
    {
        var result = await _service.UpdateAsync(
            id, CurrentUserHelper.GetUserId(User), request.FileName, file);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("InvoiceApprover")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, CurrentUserHelper.GetUserId(User));
        return Ok(new { message = "Ek silindi." });
    }
}
