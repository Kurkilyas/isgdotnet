using System.Security.Claims;
using isgDotnet.Attributes;
using isgDotnet.DTOs.Auth;
using isgDotnet.Exceptions;
using isgDotnet.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace isgDotnet.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _permissionService.GetListAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null, [FromQuery] string? name = null)
    {
        var result = await _permissionService.GetAllAsync(isActive, name);
        return Ok(result);
    }

    [HttpGet("roles/{roleId:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var result = await _permissionService.GetRolePermissionsAsync(roleId);
        return Ok(result);
    }

    [HttpPost("roles/{roleId:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> AssignToRole(int roleId, [FromBody] AssignPermissionRequestDto request)
    {
        var result = await _permissionService.AssignToRoleAsync(GetCurrentUserId(), roleId, request);
        return Ok(result);
    }

    [HttpDelete("roles/{roleId:int}/{permissionId:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> RevokeFromRole(int roleId, int permissionId)
    {
        await _permissionService.RevokeFromRoleAsync(GetCurrentUserId(), roleId, permissionId);
        return Ok(new { message = "İzin rolden alındı." });
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _permissionService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequestDto request)
    {
        var result = await _permissionService.CreateAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePermissionRequestDto request)
    {
        var result = await _permissionService.UpdateAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetPermissionActiveRequestDto request)
    {
        var result = await _permissionService.SetActiveAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _permissionService.DeleteAsync(GetCurrentUserId(), id);
        return Ok(new { message = "İzin silindi." });
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
