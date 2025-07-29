using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpPost("create_item")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        var result = await _itemService.CreateItemAsync(request);
        return result switch
        {
            CreateItemResult.Success => Ok("Item created successfully"),
            CreateItemResult.InvalidValue => BadRequest("Value is invalid"),
            CreateItemResult.ShopNotFound => NotFound("Shop not found"),
            CreateItemResult.TokenInvalid  => BadRequest("Token is invalid"),
            CreateItemResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "Database error"),
        };
    }

    [HttpPost("update_item")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateItemRequest request)
    {
        var result = await _itemService.UpdateItemAsync(request);
        return result switch
        {
            UpdateItemResult.Success => Ok("Item updated successfully"),
            UpdateItemResult.InvalidValue => BadRequest("Value is invalid"),
            UpdateItemResult.DuplicateValue => BadRequest("Value is duplicate"),
            UpdateItemResult.TokenInvalid => BadRequest("Token is invalid"),
            UpdateItemResult.ShopNotFound => NotFound("Shop not found"),
            UpdateItemResult.UserNotFound => NotFound("User not found"),
            UpdateItemResult.ItemNotFound => NotFound("Item not found"),
            _ => StatusCode(500, "Database error"),
        };
    }
    
    [HttpDelete("delete_item")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)}, {nameof(UserRole.Seller)}")]  
    public async Task<IActionResult> DeleteItem([FromBody] DeleteItemRequest request)
    {
        var result = await _itemService.DeleteItemAsync(request);
        return result switch
        {
            DeleteItemResult.Success => Ok("Item deleted successfully"),
            DeleteItemResult.ItemNotFound => NotFound("Item not found"),
            DeleteItemResult.TokenInvalid => BadRequest("Token is invalid"),
            DeleteItemResult.UserNotFound => NotFound("User not found"),
            DeleteItemResult.UserNotPermitted => NotFound("User not permitted"),
            DeleteItemResult.ShopNotFound => NotFound("Shop not found"),
            _ => StatusCode(500, "Database error"),
        };
    }
}