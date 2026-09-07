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
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _roleService.GetListAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await _roleService.GetAllAsync(isActive);
        return Ok(result);
    }

    [HttpGet("users/{userId:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetUserRoles(int userId)
    {
        var result = await _roleService.GetUserRolesAsync(userId);
        return Ok(result);
    }

    [HttpPost("users/{userId:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> AssignToUser(int userId, [FromBody] AssignRoleRequestDto request)
    {
        var result = await _roleService.AssignToUserAsync(GetCurrentUserId(), userId, request);
        return Ok(result);
    }

    [HttpDelete("users/{userId:int}/{roleId:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> RevokeFromUser(int userId, int roleId)
    {
        await _roleService.RevokeFromUserAsync(GetCurrentUserId(), userId, roleId);
        return Ok(new { message = "Rol kullanıcıdan alındı." });
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto request)
    {
        var result = await _roleService.CreateAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequestDto request)
    {
        var result = await _roleService.UpdateAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetRoleActiveRequestDto request)
    {
        var result = await _roleService.SetActiveAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roleService.DeleteAsync(GetCurrentUserId(), id);
        return Ok(new { message = "Rol silindi." });
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
