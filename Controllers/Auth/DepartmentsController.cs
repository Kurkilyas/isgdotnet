using System.Security.Claims;
using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Auth;

[ApiController]
[Route("api/departments")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _departmentService.GetListAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("InvoiceRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null, [FromQuery] string? name = null)
    {
        var result = await _departmentService.GetAllAsync(isActive, name);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _departmentService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequestDto request)
    {
        var result = await _departmentService.CreateAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequestDto request)
    {
        var result = await _departmentService.UpdateAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetDepartmentActiveRequestDto request)
    {
        var result = await _departmentService.SetActiveAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _departmentService.DeleteAsync(GetCurrentUserId(), id);
        return Ok(new { message = "Departman silindi." });
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("Oturum bilgisi geçersiz.");
        }

        return userId;
    }
}
