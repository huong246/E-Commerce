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
public class CartItemController(ICartItemService cartItemService) : ControllerBase
{

    [HttpPost("add_item_to_cart")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> AddItemToCart([FromBody] AddItemToCartRequest request)
    {
        var result = await cartItemService.AddItemToCart(request);
        return HandleResult(result);
    }

    [HttpPost("update_quantity_item_in_cart")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Customer}")]
    public async Task<IActionResult> UpdateQuantityItemInCart([FromBody] UpdateQuantityItemInCartRequest request)
    {
        var result = await cartItemService.UpdateQuantityItem(request);
        return HandleResult(result);
    }

    [HttpDelete("delete_item_from_cart")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Customer}")]
    public async Task<IActionResult> DeleteItemFromCart([FromBody] DeleteItemFromCartRequest request)
    {
        var result = await cartItemService.DeleteItemFromCart(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "CartItem deleted successfully" });
    }
    
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }
        if (typeof(T) == typeof(bool))
        {
            return NoContent(); 
        }
        return Ok(result.Value);
    }
    private IActionResult HandleFailure<T>(Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(result.Error),
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            _ => StatusCode(500, result.Error)  
        };
    }
}