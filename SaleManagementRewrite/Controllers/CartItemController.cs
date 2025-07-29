using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;
[ApiController]
[Route("[controller]")]
[Authorize]
public class CartItemController(ICartItemService cartItemService) : ControllerBase
{

    [HttpPost("add_item_to_cart")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> AddItemToCart([FromBody] AddItemToCartRequest request)
    {
        var result = await cartItemService.AddItemToCart(request);
        return result switch
        {
            AddItemToCartResult.Success => Ok("Item added to cart successfully"),
            AddItemToCartResult.InsufficientStock => BadRequest("Insufficient stock"),
            AddItemToCartResult.QuantityInvalid => BadRequest("Quantity is invalid"),
            AddItemToCartResult.ItemNotFound => BadRequest("Item not found"),
            AddItemToCartResult.OutOfStock => BadRequest("Out of stock"),
            AddItemToCartResult.ShopNotFound => BadRequest("Shop not found"),
            AddItemToCartResult.TokenInvalid => BadRequest("Token is invalid"),
            AddItemToCartResult.NotAddItemOwner  => BadRequest("Not add item owner"),
            AddItemToCartResult.UserNotFound => BadRequest("User not found"),
            _ => StatusCode(500, "Database error"),
        };
    }

    [HttpPost("update_quantity_item_in_cart")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> UpdateQuantityItemInCart([FromBody] UpdateQuantityItemInCartRequest request)
    {
        var result = await cartItemService.UpdateQuantityItem(request);
        return result switch
        {
            UpdateQuantityItemInCartResult.Success => Ok("Item updated successfully"),
            UpdateQuantityItemInCartResult.CartItemNotFound => BadRequest("Cart item not found"),
            UpdateQuantityItemInCartResult.OutOfStock => BadRequest("Out of stock"),
            UpdateQuantityItemInCartResult.TokenInvalid => BadRequest("Token is invalid"),
            UpdateQuantityItemInCartResult.UserNotFound => BadRequest("User not found"),
            UpdateQuantityItemInCartResult.InsufficientStock => BadRequest("Insufficient stock"),
            UpdateQuantityItemInCartResult.ItemNotFound => BadRequest("Item not found"),
            UpdateQuantityItemInCartResult.QuantityInvalid => BadRequest("Quantity is invalid"),
            _ => StatusCode(500, "Database error"),
        };
    }

    [HttpDelete("delete_item_from_cart")]
    [Authorize(Roles = $"{nameof(UserRole.Customer)}, {nameof(UserRole.Admin)}")]
    public async Task<IActionResult> DeleteItemFromCart([FromBody] DeleteItemFromCartRequest request)
    {
        var result = await cartItemService.DeleteItemFromCart(request);
        return result switch
        {
            DeleteItemFromCartResult.Success => Ok("Item deleted successfully"),
            DeleteItemFromCartResult.CartItemNotFound => BadRequest("Cart item not found"),
            DeleteItemFromCartResult.ItemNotFound => BadRequest("Item not found"),
            DeleteItemFromCartResult.TokenInvalid => BadRequest("Token is invalid"),
            DeleteItemFromCartResult.UserNotFound => BadRequest("User not found"),
            _ => StatusCode(500, "Database error"),
        };
    }
}