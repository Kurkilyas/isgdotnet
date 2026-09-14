using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-activity-logs")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceActivityLogsController : ControllerBase
{
    private readonly IInvoiceActivityLogService _service;

    public InvoiceActivityLogsController(IInvoiceActivityLogService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceId = null,
        [FromQuery] int? userId = null,
        [FromQuery] InvoiceActivityType? activityType = null,
        [FromQuery] string? description = null)
    {
        var result = await _service.GetListAsync(page, pageSize, invoiceId, userId, activityType, description);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetAll([FromQuery] string? name = null)
    {
        var result = await _service.GetAllAsync(name);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceId = null,
        [FromQuery] InvoiceActivityType? activityType = null,
        [FromQuery] string? description = null)
    {
        var result = await _service.GetListAsync(
            page,
            pageSize,
            invoiceId,
            CurrentUserHelper.GetUserId(User),
            activityType,
            description);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
