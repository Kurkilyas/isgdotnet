using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-types")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceTypesController : ControllerBase
{
    private readonly IInvoiceTypeService _invoiceTypeService;
    private readonly IInvoiceTypeStepService _invoiceTypeStepService;
    private readonly IWorkflowTransitionRuleService _transitionRuleService;

    public InvoiceTypesController(
        IInvoiceTypeService invoiceTypeService,
        IInvoiceTypeStepService invoiceTypeStepService,
        IWorkflowTransitionRuleService transitionRuleService)
    {
        _invoiceTypeService = invoiceTypeService;
        _invoiceTypeStepService = invoiceTypeStepService;
        _transitionRuleService = transitionRuleService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _invoiceTypeService.GetListAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await _invoiceTypeService.GetAllAsync(isActive);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _invoiceTypeService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceTypeRequestDto request)
    {
        var result = await _invoiceTypeService.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceTypeRequestDto request)
    {
        var result = await _invoiceTypeService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _invoiceTypeService.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoiceTypeService.DeleteAsync(id);
        return Ok(new { message = "Fatura türü silindi." });
    }

    [HttpGet("{invoiceTypeId:int}/steps")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetSteps(int invoiceTypeId)
    {
        var result = await _invoiceTypeStepService.GetByInvoiceTypeIdAsync(invoiceTypeId);
        return Ok(result);
    }

    [HttpPost("{invoiceTypeId:int}/steps")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> CreateStep(int invoiceTypeId, [FromBody] CreateInvoiceTypeStepRequestDto request)
    {
        var result = await _invoiceTypeStepService.CreateAsync(invoiceTypeId, request);
        return Ok(result);
    }

    [HttpGet("{invoiceTypeId:int}/transition-rules")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetTransitionRules(int invoiceTypeId)
    {
        var result = await _transitionRuleService.GetByInvoiceTypeIdAsync(invoiceTypeId);
        return Ok(result);
    }

    [HttpPost("{invoiceTypeId:int}/transition-rules")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> CreateTransitionRule(
        int invoiceTypeId,
        [FromBody] CreateWorkflowTransitionRuleRequestDto request)
    {
        var result = await _transitionRuleService.CreateAsync(invoiceTypeId, request);
        return Ok(result);
    }
}
