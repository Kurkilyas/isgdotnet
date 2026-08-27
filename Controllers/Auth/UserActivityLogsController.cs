using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class UserActivityLogsController : ControllerBase
{
    private readonly IUserActivityLogService _service;

    public UserActivityLogsController(IUserActivityLogService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] AuthActivityType? activityType = null)
    {
        var result = await _service.GetListAsync(page, pageSize, userId, activityType: activityType);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
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
