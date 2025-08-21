using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoucherController(IVoucherService voucherService) : ControllerBase
{
    [HttpPost("create_voucher")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherRequest request)
    {
        var result = await voucherService.CreateVoucher(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return CreatedAtAction(nameof(GetVoucherById),  new { id = result.Value!.Id }, result.Value);
    }
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> GetVoucherById(Guid id)
    {
        var voucher = await voucherService.GetVoucherByIdAsync(id);
        if (voucher is null)
        {
            return NotFound();
        }
        return Ok(voucher);
    }
    [HttpDelete("delete_voucher")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> DeleteVoucher([FromBody] DeleteVoucherRequest request)
    {
        var result = await voucherService.DeleteVoucher(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }

        return Ok(new { message = "Voucher deleted successfully" });
    }
    [HttpPost("update_voucher")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> UpdateVoucher([FromBody] UpdateVoucherRequest request)
    {
        var result = await voucherService.UpdateVoucher(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return CreatedAtAction(nameof(GetVoucherById),  new { id = result.Value!.Id }, result.Value);
    }
}