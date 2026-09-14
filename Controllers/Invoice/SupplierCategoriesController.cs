using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/supplier-categories")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class SupplierCategoriesController : ControllerBase
{
    private readonly ISupplierCategoryService _supplierCategoryService;

    public SupplierCategoriesController(ISupplierCategoryService supplierCategoryService)
    {
        _supplierCategoryService = supplierCategoryService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _supplierCategoryService.GetListAsync(page, pageSize, search, isActive);
        return Ok(result);
    }

    [HttpGet("all")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null, [FromQuery] string? name = null)
    {
        var result = await _supplierCategoryService.GetAllAsync(isActive, name);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _supplierCategoryService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCategoryRequestDto request)
    {
        var result = await _supplierCategoryService.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierCategoryRequestDto request)
    {
        var result = await _supplierCategoryService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _supplierCategoryService.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _supplierCategoryService.DeleteAsync(id);
        return Ok(new { message = "Tedarikçi kategorisi silindi." });
    }
}
