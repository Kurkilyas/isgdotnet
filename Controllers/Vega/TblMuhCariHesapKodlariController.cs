using InvoiceTrackingSystemBackend.Interfaces.Vega;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackingSystemBackend.Controllers.Vega;

[ApiController]
[Route("api/[controller]")]
public class TblMuhCariHesapKodlariController : ControllerBase
{
    private readonly ITblMuhCariHesapKodlariService _service;

    public TblMuhCariHesapKodlariController(ITblMuhCariHesapKodlariService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetListAsync(page, pageSize);
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
