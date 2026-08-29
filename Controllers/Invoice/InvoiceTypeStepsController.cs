using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-type-steps")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceTypeStepsController : ControllerBase
{
    private readonly IInvoiceTypeStepService _invoiceTypeStepService;
    private readonly IInvoiceTypeStepApproverService _approverService;

    public InvoiceTypeStepsController(
        IInvoiceTypeStepService invoiceTypeStepService,
        IInvoiceTypeStepApproverService approverService)
    {
        _invoiceTypeStepService = invoiceTypeStepService;
        _approverService = approverService;
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _invoiceTypeStepService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceTypeStepRequestDto request)
    {
        var result = await _invoiceTypeStepService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _invoiceTypeStepService.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _invoiceTypeStepService.DeleteAsync(id);
        return Ok(new { message = "Fatura türü adımı silindi." });
    }

    [HttpGet("{stepId:int}/approvers")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetApprovers(int stepId)
    {
        var result = await _approverService.GetByStepIdAsync(stepId);
        return Ok(result);
    }

    [HttpPost("{stepId:int}/approvers")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> CreateApprover(int stepId, [FromBody] CreateInvoiceTypeStepApproverRequestDto request)
    {
        var result = await _approverService.CreateAsync(stepId, request);
        return Ok(result);
    }
}
