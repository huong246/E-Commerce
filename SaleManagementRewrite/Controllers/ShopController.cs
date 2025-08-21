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
public class ShopController(IShopService shopService) : ControllerBase
{
    [HttpPost("create_shop")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
    {
        var result = await shopService.CreateShop(request);
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
        return CreatedAtAction(nameof(GetShopById),  new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> GetShopById(Guid id)
    {
        var shop = await shopService.GetShopByIdAsync(id);
        if (shop is null)
        {
            return NotFound();
        }
        return Ok(shop);
    }
    [HttpPost("update_shop")]
    [Authorize(Roles = $" {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> UpdateShop([FromBody] UpdateShopRequest request)
    {
        var result = await shopService.UpdateShop(request);
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
        return CreatedAtAction(nameof(GetShopById),  new { id = result.Value!.Id }, result.Value);
    }
    
}