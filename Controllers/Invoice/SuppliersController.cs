using InvoiceTrackingSystemBackend.Attributes;
using InvoiceTrackingSystemBackend.DTOs.Invoice;
using InvoiceTrackingSystemBackend.Interfaces.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceTrackingSystemBackend.Controllers.Invoice;

[ApiController]
[Route("api/suppliers")]
[Authorize]
[EnableRateLimiting("GlobalLimit")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? supplierCategoryId = null)
    {
        var result = await _supplierService.GetListAsync(page, pageSize, search, isActive, supplierCategoryId);
        return Ok(result);
    }

    [HttpGet("all")]
   [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var result = await _supplierService.GetAllAsync(isActive);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _supplierService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequestDto request)
    {
        var result = await _supplierService.CreateAsync(request);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequestDto request)
    {
        var result = await _supplierService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> SetActive(int id, [FromBody] SetActiveRequestDto request)
    {
        var result = await _supplierService.SetActiveAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);
        return Ok(new { message = "Tedarikçi silindi." });
    }

    [HttpGet("{id:int}/invoice-types")]
    [Permission("AdminRead")]
    public async Task<IActionResult> GetInvoiceTypes(int id)
    {
        var result = await _supplierService.GetInvoiceTypesAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:int}/invoice-types")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> AssignInvoiceTypes(int id, [FromBody] AssignSupplierInvoiceTypesRequestDto request)
    {
        var result = await _supplierService.AssignInvoiceTypesAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:int}/invoice-types/{invoiceTypeId:int}")]
    [Permission("AdminWrite")]
    public async Task<IActionResult> RevokeInvoiceType(int id, int invoiceTypeId)
    {
        await _supplierService.RevokeInvoiceTypeAsync(id, invoiceTypeId);
        return Ok(new { message = "Fatura türü tedarikçiden alındı." });
    }
}
