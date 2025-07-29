using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopController(IShopService shopService) : ControllerBase
{
    [HttpPost("create_shop")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
    {
        var result = await shopService.CreateShop(request);
        return result switch
        {
            CreateShopResult.Success => Ok("Shop created successfully"),
            CreateShopResult.UserNotFound => NotFound("User not found"),
            CreateShopResult.TokenInvalid  => BadRequest("Token invalid"),
            CreateShopResult.UserHasShop => Conflict("User has a shop"),
            CreateShopResult.BecomeASeller => BadRequest("You are not seller, become A Seller"),
            _ => StatusCode(500, "DatabaseError"),
        };
    }

    [HttpPost("update_shop")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> UpdateShop([FromBody] UpdateShopRequest request)
    {
        var result = await shopService.UpdateShop(request);
        return result switch
        {
            UpdateShopResult.Success => Ok("Shop updated successfully"),
            UpdateShopResult.TokenInvalid => BadRequest("Token invalid"),
            UpdateShopResult.ShopNotFound => NotFound("Shop not found"),
            UpdateShopResult.UserNotFound => NotFound("User not found"),
            UpdateShopResult.DuplicateValue => BadRequest("Duplicate value"),
            UpdateShopResult.AddressNotFound  => NotFound("Address not found"),
            _ => StatusCode(500, "DatabaseError"),
        };
    }
    
}