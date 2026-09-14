using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/workflow-transition-rules")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class WorkflowTransitionRulesController : ControllerBase
{
    private readonly IWorkflowTransitionRuleService _service;

    public WorkflowTransitionRulesController(IWorkflowTransitionRuleService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? invoiceTypeId = null,
        [FromQuery] WorkflowActionType? triggerAction = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _service.GetListAsync(page, pageSize, invoiceTypeId, triggerAction, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null, [FromQuery] string? name = null)
    {
        var result = await _service.GetAllAsync(isActive, name);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkflowTransitionRuleRequestDto request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _service.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Geçiş kuralı silindi." });
    }
}
