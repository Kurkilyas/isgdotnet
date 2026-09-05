using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-workflow-histories")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceWorkflowHistoriesController : ControllerBase
{
    private readonly IInvoiceWorkflowHistoryService _service;

    public InvoiceWorkflowHistoriesController(IInvoiceWorkflowHistoryService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceId = null,
        [FromQuery] int? actorUserId = null,
        [FromQuery] WorkflowActionType? actionType = null)
    {
        var result = await _service.GetListAsync(page, pageSize, invoiceId, actorUserId, actionType);
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
