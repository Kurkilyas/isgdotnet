using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackingSystemBackend.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class UserActivityLogsController : ControllerBase
{
    private readonly IUserActivityLogService _service;

    public UserActivityLogsController(IUserActivityLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] AuthActivityType? activityType = null)
    {
        var result = await _service.GetListAsync(page, pageSize, userId, activityType);
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
