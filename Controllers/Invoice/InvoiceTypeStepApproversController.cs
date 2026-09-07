using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/invoice-type-step-approvers")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class InvoiceTypeStepApproversController : ControllerBase
{
    private readonly IInvoiceTypeStepApproverService _approverService;

    public InvoiceTypeStepApproversController(IInvoiceTypeStepApproverService approverService)
    {
        _approverService = approverService;
    }

    [HttpGet("all")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await _approverService.GetAllAsync(isActive);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceTypeStepApproverRequestDto request)
    {
        var result = await _approverService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _approverService.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _approverService.DeleteAsync(id);
        return Ok(new { message = "Onaylayıcı adımdan alındı." });
    }
}
