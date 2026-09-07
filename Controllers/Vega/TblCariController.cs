using InvoiceTrackingSystemBackend.Interfaces.Vega;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Vega;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class TblCariController : ControllerBase
{
    private readonly ITblCariService _service;

    public TblCariController(ITblCariService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetListAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await _service.GetAllAsync(isActive);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
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
