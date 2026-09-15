using System.Security.Claims;
using isgDotnet.Attributes;
using isgDotnet.Constants;
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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _userService.GetMeAsync(GetCurrentUserId());
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequestDto request)
    {
        var result = await _userService.UpdateMeAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        await _userService.ChangePasswordAsync(GetCurrentUserId(), request);
        return Ok(new { message = "Şifre güncellendi." });
    }

    [HttpPut("me/out-of-office")]
    public async Task<IActionResult> UpdateOutOfOffice([FromBody] UpdateOutOfOfficeRequestDto request)
    {
        var result = await _userService.UpdateOutOfOfficeAsync(GetCurrentUserId(), request);
        return Ok(result);
    }

    [HttpGet("me/files")]
    public async Task<IActionResult> GetMyFiles()
    {
        var result = await _userService.GetMyFilesAsync(GetCurrentUserId());
        return Ok(result);
    }

    [HttpPost("me/files")]
    [RequestSizeLimit(2 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> UploadMyFile([FromForm] UserFileKind fileKind, IFormFile file)
    {
        var result = await _userService.UploadMyFileAsync(GetCurrentUserId(), fileKind, file);
        return Ok(result);
    }

    [HttpGet("me/files/{fileKind}")]
    public async Task<IActionResult> GetMyFileMeta(UserFileKind fileKind)
    {
        var result = await _userService.GetMyFileMetaAsync(GetCurrentUserId(), fileKind);
        return Ok(result);
    }

    [HttpGet("me/files/{fileKind}/content")]
    public async Task<IActionResult> GetMyFileContent(UserFileKind fileKind)
    {
        var (content, contentType, _) = await _userService.OpenMyFileAsync(GetCurrentUserId(), fileKind);
        return File(content, contentType);
    }

    [HttpDelete("me/files/{fileKind}")]
    public async Task<IActionResult> DeleteMyFile(UserFileKind fileKind)
    {
        await _userService.DeleteMyFileAsync(GetCurrentUserId(), fileKind);
        return Ok(new { message = "Dosya silindi." });
    }

    [HttpGet]
    [Permission("UserRead", AllowDepartmentManager = true)]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetListAsync(GetCurrentUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null, [FromQuery] string? name = null)
    {
        var result = await _userService.GetAllAsync(isActive, name);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("UserRead", AllowDepartmentManager = true)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(GetCurrentUserId(), id);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("UserWrite", AllowDepartmentManager = true)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
    {
        var result = await _userService.UpdateUserAsync(GetCurrentUserId(), id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("UserWrite", AllowDepartmentManager = true)]
    public async Task<IActionResult> SetUserActive(int id, [FromBody] SetUserActiveRequestDto request)
    {
        var result = await _userService.SetUserActiveAsync(GetCurrentUserId(), id, request);
        return Ok(result);
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
