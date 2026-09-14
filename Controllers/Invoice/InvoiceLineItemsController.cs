using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-line-items")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceLineItemsController : ControllerBase
{
    private readonly IInvoiceLineItemService _service;

    public InvoiceLineItemsController(IInvoiceLineItemService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetListAsync(page, pageSize, invoiceId, search);
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

    [HttpPost]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceLineItemRequestDto request)
    {
        var result = await _service.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceLineItemRequestDto request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("InvoiceWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Fatura kalemi silindi." });
    }
}
